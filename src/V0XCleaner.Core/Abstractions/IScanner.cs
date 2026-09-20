using V0XCleaner.Core.Models;

namespace V0XCleaner.Core.Abstractions;

/// <summary>
/// Analyse une portion du système (fichiers, registre, navigateurs...) et retourne
/// les éléments candidats au nettoyage, sans jamais rien modifier.
/// </summary>
public interface IScanner
{
    /// <summary>Nom stable identifiant ce scanner (ex: "system-temp", "browser-chrome").</summary>
    string Key { get; }

    /// <summary>Catégorie principale produite par ce scanner (pour le regroupement UI).</summary>
    CleanupCategory Category { get; }

    Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default);
}
