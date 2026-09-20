using Microsoft.Win32;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.Services.Scanners;

/// <summary>
/// Historique récent stocké dans le registre utilisateur : boîte "Exécuter" (RunMRU)
/// et chemins tapés dans l'explorateur (TypedPaths). Ne touche pas à RecentDocs
/// (structure plus complexe, par extension de fichier) dans cette version.
/// </summary>
public sealed class RegistryMruScanner : IScanner
{
    internal static readonly string[] SubKeyPaths =
    [
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU",
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\TypedPaths",
    ];

    public string Key => "registry-mru";

    public CleanupCategory Category => CleanupCategory.ClipboardAndMru;

    public Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<CleanupItem>();

        foreach (var subKeyPath in SubKeyPaths)
        {
            using var key = Registry.CurrentUser.OpenSubKey(subKeyPath, writable: false);
            if (key is null)
            {
                continue;
            }

            foreach (var valueName in key.GetValueNames())
            {
                items.Add(new CleanupItem
                {
                    Id = $@"HKCU\{subKeyPath}\{valueName}",
                    DisplayPath = $@"HKCU\{subKeyPath}\{valueName}",
                    Category = Category,
                    SizeBytes = 0,
                    Description = "Historique récent (boîte Exécuter / chemins tapés dans l'Explorateur)."
                });
            }
        }

        return Task.FromResult(new ScanResult { Items = items });
    }
}
