using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.FileSystem;
using V0XCleaner.Services.Native;

namespace V0XCleaner.Services.Scanners;

/// <summary>
/// Raccourcis (.lnk) du Bureau et du menu Démarrer dont la cible n'existe plus. Les cibles
/// vides ou "virtuelles" (dossiers spéciaux shell, ex: "::{GUID}") sont ignorées par prudence.
/// Produit des CleanupItem "fichier" classiques : nettoyé par le FileDeletionCleaner générique.
/// </summary>
public sealed class BrokenShortcutsScanner : IScanner
{
    private static readonly string[] SearchRoots =
    [
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
        Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
        Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)
    ];

    public string Key => "registry-broken-shortcuts";

    public CleanupCategory Category => CleanupCategory.Registry;

    public Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<CleanupItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in SearchRoots.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                continue;
            }

            foreach (var lnkFile in FileSystemHelpers.EnumerateFilesSafe(root))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!lnkFile.Extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!seen.Add(lnkFile.FullName))
                {
                    continue;
                }

                var target = ShellLinkResolver.ResolveTarget(lnkFile.FullName);
                if (string.IsNullOrWhiteSpace(target) || target.StartsWith("::", StringComparison.Ordinal))
                {
                    continue;
                }

                var expanded = Environment.ExpandEnvironmentVariables(target);
                if (File.Exists(expanded) || Directory.Exists(expanded))
                {
                    continue;
                }

                items.Add(new CleanupItem
                {
                    Id = lnkFile.FullName,
                    DisplayPath = lnkFile.FullName,
                    Category = Category,
                    SizeBytes = FileSystemHelpers.GetFileSizeSafe(lnkFile.FullName),
                    Description = $"Le raccourci pointe vers une cible introuvable : {expanded}"
                });
            }
        }

        return Task.FromResult(new ScanResult { Items = items });
    }
}
