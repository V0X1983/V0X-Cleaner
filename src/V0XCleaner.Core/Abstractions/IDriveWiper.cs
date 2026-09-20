using V0XCleaner.Core.Models;

namespace V0XCleaner.Core.Abstractions;

/// <summary>
/// Efface de façon sécurisée l'ESPACE LIBRE d'un lecteur (jamais les fichiers existants) en le
/// remplissant temporairement de données, pour empêcher la récupération de fichiers déjà
/// supprimés dont les secteurs n'ont pas encore été réécrits.
/// </summary>
public interface IDriveWiper
{
    Task WipeFreeSpaceAsync(
        string driveRoot,
        WipeMethod method,
        IProgress<WipeProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
