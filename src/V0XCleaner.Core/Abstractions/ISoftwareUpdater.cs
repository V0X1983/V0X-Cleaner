using V0XCleaner.Core.Models;

namespace V0XCleaner.Core.Abstractions;

/// <summary>Détecte et applique les mises à jour de logiciels via le gestionnaire de paquets Windows (winget).</summary>
public interface ISoftwareUpdater
{
    Task<SoftwareUpdateScan> ScanAsync(CancellationToken cancellationToken = default);

    Task<SoftwareUpdateResult> UpdateAsync(string packageId, IProgress<SoftwareUpdateProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>Retourne l'adresse https du site de l'éditeur du paquet, ou null si elle est introuvable.</summary>
    Task<Uri?> GetWebsiteAsync(string packageId, CancellationToken cancellationToken = default);
}
