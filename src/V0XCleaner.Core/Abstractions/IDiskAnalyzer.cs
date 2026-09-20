using V0XCleaner.Core.Models;

namespace V0XCleaner.Core.Abstractions;

/// <summary>
/// Calcule la taille des éléments d'un dossier (analyse "à la demande", niveau par niveau,
/// plutôt qu'un arbre complet d'un coup) pour naviguer un disque comme un explorateur.
/// </summary>
public interface IDiskAnalyzer
{
    IReadOnlyList<string> GetAvailableDrives();

    Task<IReadOnlyList<FolderSizeNode>> AnalyzeAsync(string path, CancellationToken cancellationToken = default);
}
