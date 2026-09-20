using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.FileSystem;

namespace V0XCleaner.Services.Scanners;

/// <summary>
/// Scanner générique piloté par une liste de motifs de chemins (voir <see cref="PathPatternExpander"/>).
/// Couvre la grande majorité des cas de nettoyage : chaque motif résolu qui pointe vers un fichier
/// devient un CleanupItem ; chaque motif résolu qui pointe vers un dossier voit tout son contenu
/// énuméré fichier par fichier (pour un suivi et un log précis lors de la suppression).
/// </summary>
public sealed class PathPatternScanner : IScanner
{
    private readonly IReadOnlyList<string> _patterns;

    public PathPatternScanner(string key, CleanupCategory category, IReadOnlyList<string> patterns)
    {
        Key = key;
        Category = category;
        _patterns = patterns;
    }

    public string Key { get; }

    public CleanupCategory Category { get; }

    public Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var items = new List<CleanupItem>();

        foreach (var pattern in _patterns)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var resolvedPath in PathPatternExpander.Expand(pattern))
            {
                if (File.Exists(resolvedPath))
                {
                    AddFile(resolvedPath, seen, items);
                }
                else if (Directory.Exists(resolvedPath))
                {
                    foreach (var file in FileSystemHelpers.EnumerateFilesSafe(resolvedPath))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        AddFile(file.FullName, seen, items);
                    }
                }
            }
        }

        return Task.FromResult(new ScanResult { Items = items });
    }

    private void AddFile(string fullPath, HashSet<string> seen, List<CleanupItem> items)
    {
        if (!seen.Add(fullPath))
        {
            return;
        }

        items.Add(new CleanupItem
        {
            Id = fullPath,
            DisplayPath = fullPath,
            Category = Category,
            SizeBytes = FileSystemHelpers.GetFileSizeSafe(fullPath)
        });
    }
}
