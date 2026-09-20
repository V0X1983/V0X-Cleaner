using System.Security.Cryptography;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.FileSystem;

namespace V0XCleaner.Services;

/// <summary>
/// Recherche de doublons en deux passes : regroupement par taille (rapide, élimine l'immense
/// majorité des fichiers uniques sans lire leur contenu), puis hash SHA-256 uniquement pour les
/// fichiers dont au moins un autre partage exactement la même taille.
/// </summary>
public sealed class DuplicateFileFinder : IDuplicateFileFinder
{
    public async Task<IReadOnlyList<DuplicateFileGroup>> FindDuplicatesAsync(
        string rootPath,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var allFiles = FileSystemHelpers.EnumerateFilesSafe(rootPath).ToList();
        progress?.Report($"{allFiles.Count} fichier(s) trouvés, regroupement par taille...");

        var sizeGroups = allFiles
            .GroupBy(f => FileSystemHelpers.GetFileSizeSafe(f.FullName))
            .Where(g => g.Key > 0 && g.Count() > 1)
            .ToList();

        var result = new List<DuplicateFileGroup>();
        var processed = 0;

        foreach (var sizeGroup in sizeGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var byHash = new Dictionary<string, List<string>>();

            foreach (var file in sizeGroup)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hash = await ComputeHashAsync(file.FullName, cancellationToken);
                if (hash is not null)
                {
                    if (!byHash.TryGetValue(hash, out var list))
                    {
                        list = [];
                        byHash[hash] = list;
                    }

                    list.Add(file.FullName);
                }

                processed++;
                if (processed % 25 == 0)
                {
                    progress?.Report($"{processed} fichier(s) analysés...");
                }
            }

            foreach (var group in byHash.Values.Where(g => g.Count > 1))
            {
                result.Add(new DuplicateFileGroup { SizeBytes = sizeGroup.Key, FilePaths = group });
            }
        }

        return result.OrderByDescending(g => g.WastedBytes).ToList();
    }

    private static async Task<string?> ComputeHashAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
            return Convert.ToHexString(hashBytes);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
