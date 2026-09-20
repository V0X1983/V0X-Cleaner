using Microsoft.Win32;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.RegistryCleanup;

namespace V0XCleaner.Services.Scanners;

/// <summary>
/// Classes COM/ActiveX (HKEY_CLASSES_ROOT\CLSID\{GUID}) dont le serveur local (InprocServer32 /
/// LocalServer32) pointe vers un fichier absent. Par prudence, seules les références à un
/// chemin ABSOLU manquant sont signalées : un simple nom de DLL (résolu par le chemin de
/// recherche Windows) n'est jamais flaggé pour éviter tout faux positif sur des composants système.
/// </summary>
public sealed class OrphanedComClsidScanner : IScanner
{
    public string Key => "registry-com-clsid";

    public CleanupCategory Category => CleanupCategory.Registry;

    public Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<CleanupItem>();

        using var clsidRoot = RegistrySafe.OpenSubKey(Registry.ClassesRoot, "CLSID");
        if (clsidRoot is null)
        {
            return Task.FromResult(new ScanResult { Items = items });
        }

        foreach (var clsid in RegistrySafe.GetSubKeyNames(clsidRoot))
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var clsidKey = RegistrySafe.OpenSubKey(clsidRoot, clsid);
            if (clsidKey is null)
            {
                continue;
            }

            var serverPath = GetServerDefaultValue(clsidKey, "InprocServer32") ?? GetServerDefaultValue(clsidKey, "LocalServer32");
            if (serverPath is null || !IsOrphaned(serverPath))
            {
                continue;
            }

            items.Add(new CleanupItem
            {
                Id = $@"HKCR\CLSID\{clsid}",
                DisplayPath = $@"HKEY_CLASSES_ROOT\CLSID\{clsid}",
                Category = Category,
                SizeBytes = 0,
                Description = $"La classe COM {clsid} référence le composant manquant \"{serverPath}\"."
            });
        }

        return Task.FromResult(new ScanResult { Items = items });
    }

    private static string? GetServerDefaultValue(RegistryKey clsidKey, string subKeyName)
    {
        using var serverKey = RegistrySafe.OpenSubKey(clsidKey, subKeyName);
        if (serverKey is null)
        {
            return null;
        }

        var value = RegistrySafe.GetValue(serverKey, null) as string;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool IsOrphaned(string rawValue)
    {
        var path = ExtractExecutablePath(rawValue);
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var expanded = Environment.ExpandEnvironmentVariables(path);
        if (!Path.IsPathRooted(expanded))
        {
            // Nom de fichier seul : résolu via le chemin de recherche Windows, on ne peut pas
            // vérifier fiablement sans reproduire cette logique. Par prudence, on ne signale pas.
            return false;
        }

        try
        {
            return !File.Exists(expanded);
        }
        catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException)
        {
            return false;
        }
    }

    private static string? ExtractExecutablePath(string rawValue)
    {
        var trimmed = rawValue.Trim().Trim('"');
        if (trimmed.Length == 0)
        {
            return null;
        }

        var exeIdx = trimmed.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        var dllIdx = trimmed.IndexOf(".dll", StringComparison.OrdinalIgnoreCase);

        if (exeIdx >= 0)
        {
            return trimmed[..(exeIdx + 4)];
        }

        if (dllIdx >= 0)
        {
            return trimmed[..(dllIdx + 4)];
        }

        return trimmed;
    }
}
