using Microsoft.Win32;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.RegistryCleanup;

namespace V0XCleaner.Services.Scanners;

/// <summary>
/// Cache MuiCache (noms d'affichage d'applications mémorisés par l'Explorateur) pour des
/// programmes déjà désinstallés. Seules les entrées dont le nom de valeur commence par un
/// chemin absolu reconnaissable (se terminant par .exe/.dll/...) sont analysées.
/// </summary>
public sealed class OrphanedMuiCacheScanner : IScanner
{
    private const string KeyPath = @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache";
    private static readonly string[] Extensions = [".exe", ".dll", ".cpl", ".ax", ".ocx"];

    public string Key => "registry-mui-cache";

    public CleanupCategory Category => CleanupCategory.Registry;

    public Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<CleanupItem>();

        using var key = RegistrySafe.OpenSubKey(Registry.CurrentUser, KeyPath);
        if (key is not null)
        {
            foreach (var valueName in RegistrySafe.GetValueNames(key))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var extractedPath = ExtractPath(valueName);
                if (extractedPath is null)
                {
                    continue;
                }

                var expanded = Environment.ExpandEnvironmentVariables(extractedPath);
                if (File.Exists(expanded))
                {
                    continue;
                }

                items.Add(new CleanupItem
                {
                    Id = $@"HKCU\{KeyPath}||{valueName}",
                    DisplayPath = $@"HKEY_CURRENT_USER\{KeyPath}",
                    Category = Category,
                    SizeBytes = 0,
                    Description = $"Entrée de cache d'applications pour un programme désinstallé : {expanded}"
                });
            }
        }

        return Task.FromResult(new ScanResult { Items = items });
    }

    private static string? ExtractPath(string valueName)
    {
        if (!Path.IsPathRooted(valueName))
        {
            return null;
        }

        foreach (var ext in Extensions)
        {
            var idx = valueName.IndexOf(ext, StringComparison.OrdinalIgnoreCase);
            if (idx > 0)
            {
                return valueName[..(idx + ext.Length)];
            }
        }

        return null;
    }
}
