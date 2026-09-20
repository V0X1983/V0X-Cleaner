using Microsoft.Win32;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.RegistryCleanup;

namespace V0XCleaner.Services.Scanners;

/// <summary>
/// Extensions de fichiers (HKEY_CLASSES_ROOT\.ext) dont le ProgID référencé par la valeur par
/// défaut ne correspond à aucune clé existante.
/// </summary>
public sealed class OrphanedFileExtensionsScanner : IScanner
{
    public string Key => "registry-file-extensions";

    public CleanupCategory Category => CleanupCategory.Registry;

    public Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<CleanupItem>();
        var classesRoot = Registry.ClassesRoot;

        foreach (var subKeyName in RegistrySafe.GetSubKeyNames(classesRoot))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!subKeyName.StartsWith('.'))
            {
                continue;
            }

            using var extKey = RegistrySafe.OpenSubKey(classesRoot, subKeyName);
            if (extKey is null)
            {
                continue;
            }

            if (RegistrySafe.GetValue(extKey, null) is not string progId || string.IsNullOrWhiteSpace(progId))
            {
                continue;
            }

            using var progIdKey = RegistrySafe.OpenSubKey(classesRoot, progId);
            if (progIdKey is not null)
            {
                continue;
            }

            items.Add(new CleanupItem
            {
                Id = $@"HKCR\{subKeyName}",
                DisplayPath = $@"HKEY_CLASSES_ROOT\{subKeyName}",
                Category = Category,
                SizeBytes = 0,
                Description = $"L'extension \"{subKeyName}\" référence le type de fichier manquant \"{progId}\".",
                RequiresElevatedConfirmation = true
            });
        }

        return Task.FromResult(new ScanResult { Items = items });
    }
}
