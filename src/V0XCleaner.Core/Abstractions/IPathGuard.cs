namespace V0XCleaner.Core.Abstractions;

/// <summary>
/// Dernier filet de sécurité vérifié juste avant toute suppression réelle de fichier/dossier.
/// Indépendant de la logique de chaque scanner : même si un scanner contient un bug et produit
/// un chemin dangereux (racine de disque, dossier Windows, Program Files...), ce garde-fou
/// doit refuser l'opération.
/// </summary>
public interface IPathGuard
{
    /// <summary>
    /// Retourne false si <paramref name="fullPath"/> ne doit jamais être supprimé automatiquement.
    /// </summary>
    bool IsSafeToDelete(string fullPath, out string? reason);
}
