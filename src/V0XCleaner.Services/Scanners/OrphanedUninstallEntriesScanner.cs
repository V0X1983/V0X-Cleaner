using Microsoft.Win32;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.RegistryCleanup;

namespace V0XCleaner.Services.Scanners;

/// <summary>
/// Entrées "Programmes et fonctionnalités" (…\Uninstall\{clé}) dont le désinstalleur (exe) n'existe
/// plus. Les composants systèmes (SystemComponent=1) et les entrées gérées par Windows Installer
/// (UninstallString contenant msiexec) sont ignorés : ces derniers restent valides tant que le
/// produit MSI est enregistré, indépendamment du chemin affiché.
/// </summary>
public sealed class OrphanedUninstallEntriesScanner : IScanner
{
    private static readonly (RegistryKey Hive, string HivePrefix, string SubPath)[] Roots =
    [
        (Registry.LocalMachine, "HKLM", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
        (Registry.LocalMachine, "HKLM", @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
        (Registry.CurrentUser, "HKCU", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")
    ];

    public string Key => "registry-uninstallers";

    public CleanupCategory Category => CleanupCategory.Registry;

    public Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<CleanupItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (hive, hivePrefix, subPath) in Roots)
        {
            using var baseKey = RegistrySafe.OpenSubKey(hive, subPath);
            if (baseKey is null)
            {
                continue;
            }

            foreach (var subName in RegistrySafe.GetSubKeyNames(baseKey))
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var subKey = RegistrySafe.OpenSubKey(baseKey, subName);
                if (subKey is null)
                {
                    continue;
                }

                if (RegistrySafe.GetValue(subKey, "SystemComponent") is int systemComponent && systemComponent == 1)
                {
                    continue;
                }

                if (RegistrySafe.GetValue(subKey, "UninstallString") is not string uninstallString || string.IsNullOrWhiteSpace(uninstallString))
                {
                    continue;
                }

                if (uninstallString.Contains("msiexec", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var exePath = ExtractExecutablePath(uninstallString);
                if (exePath is null || File.Exists(exePath))
                {
                    continue;
                }

                var id = $@"{hivePrefix}\{subPath}\{subName}";
                if (!seen.Add(id))
                {
                    continue;
                }

                var displayName = RegistrySafe.GetValue(subKey, "DisplayName") as string ?? subName;
                items.Add(new CleanupItem
                {
                    Id = id,
                    DisplayPath = $@"{(hivePrefix == "HKLM" ? "HKEY_LOCAL_MACHINE" : "HKEY_CURRENT_USER")}\{subPath}\{subName}",
                    Category = Category,
                    SizeBytes = 0,
                    Description = $"Désinstalleur fantôme pour \"{displayName}\" (programme déjà supprimé).",
                    RequiresElevatedConfirmation = hivePrefix == "HKLM"
                });
            }
        }

        return Task.FromResult(new ScanResult { Items = items });
    }

    private static string? ExtractExecutablePath(string uninstallString)
    {
        var trimmed = uninstallString.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (trimmed[0] == '"')
        {
            var end = trimmed.IndexOf('"', 1);
            return end > 1 ? Environment.ExpandEnvironmentVariables(trimmed[1..end]) : null;
        }

        var idx = trimmed.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        return idx > 0 ? Environment.ExpandEnvironmentVariables(trimmed[..(idx + 4)]) : null;
    }
}
