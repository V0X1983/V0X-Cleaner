using V0XCleaner.Core.Models;

namespace V0XCleaner.Core.Abstractions;

/// <summary>Recherche les fichiers en double sous un dossier : d'abord par taille, puis par hash SHA-256.</summary>
public interface IDuplicateFileFinder
{
    Task<IReadOnlyList<DuplicateFileGroup>> FindDuplicatesAsync(
        string rootPath,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
