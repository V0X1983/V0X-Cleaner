using Microsoft.Win32;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.RegistryCleanup;

namespace V0XCleaner.Services.Scanners;

/// <summary>App Paths (…\CurrentVersion\App Paths\app.exe) dont l'exécutable référencé n'existe plus.</summary>
public sealed class OrphanedAppPathsScanner : IScanner
{
    private const string SubPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";

    private static readonly (RegistryKey Hive, string HivePrefix)[] Roots =
    [
        (Registry.LocalMachine, "HKLM"),
        (Registry.CurrentUser, "HKCU")
    ];

    public string Key => "registry-app-paths";

    public CleanupCategory Category => CleanupCategory.Registry;

    public Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<CleanupItem>();

        foreach (var (hive, hivePrefix) in Roots)
        {
            using var baseKey = RegistrySafe.OpenSubKey(hive, SubPath);
            if (baseKey is null)
            {
                continue;
            }

            foreach (var appName in RegistrySafe.GetSubKeyNames(baseKey))
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var appKey = RegistrySafe.OpenSubKey(baseKey, appName);
                if (appKey is null)
                {
                    continue;
                }

                if (RegistrySafe.GetValue(appKey, null) is not string exePath || string.IsNullOrWhiteSpace(exePath))
                {
                    continue;
                }

                var expanded = Environment.ExpandEnvironmentVariables(exePath.Trim('"'));
                if (File.Exists(expanded))
                {
                    continue;
                }

                items.Add(new CleanupItem
                {
                    Id = $@"{hivePrefix}\{SubPath}\{appName}",
                    DisplayPath = $@"{(hivePrefix == "HKLM" ? "HKEY_LOCAL_MACHINE" : "HKEY_CURRENT_USER")}\{SubPath}\{appName}",
                    Category = Category,
                    SizeBytes = 0,
                    Description = $"Le chemin d'application enregistré pour \"{appName}\" est introuvable : {expanded}",
                    RequiresElevatedConfirmation = hivePrefix == "HKLM"
                });
            }
        }

        return Task.FromResult(new ScanResult { Items = items });
    }
}
