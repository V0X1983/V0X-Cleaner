using Microsoft.Win32;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.RegistryCleanup;

namespace V0XCleaner.Services.Scanners;

/// <summary>Polices enregistrées dont le fichier (dans %SystemRoot%\Fonts, ou chemin absolu) n'existe plus.</summary>
public sealed class OrphanedFontsScanner : IScanner
{
    private const string KeyPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts";

    public string Key => "registry-fonts";

    public CleanupCategory Category => CleanupCategory.Registry;

    public Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<CleanupItem>();

        using var key = RegistrySafe.OpenSubKey(Registry.LocalMachine, KeyPath);
        if (key is not null)
        {
            var fontsFolder = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\Fonts");

            foreach (var valueName in RegistrySafe.GetValueNames(key))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(valueName))
                {
                    continue;
                }

                if (RegistrySafe.GetValue(key, valueName) is not string fileName || string.IsNullOrWhiteSpace(fileName))
                {
                    continue;
                }

                var fullPath = Path.IsPathRooted(fileName) ? fileName : Path.Combine(fontsFolder, fileName);
                if (File.Exists(fullPath))
                {
                    continue;
                }

                items.Add(new CleanupItem
                {
                    Id = $@"HKLM\{KeyPath}||{valueName}",
                    DisplayPath = $@"HKEY_LOCAL_MACHINE\{KeyPath}",
                    Category = Category,
                    SizeBytes = 0,
                    Description = $"La police \"{valueName}\" référence le fichier introuvable : {fileName}"
                });
            }
        }

        return Task.FromResult(new ScanResult { Items = items });
    }
}
