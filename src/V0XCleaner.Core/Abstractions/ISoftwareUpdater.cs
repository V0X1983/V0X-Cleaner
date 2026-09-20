using V0XCleaner.Core.Models;

namespace V0XCleaner.Core.Abstractions;

/// <summary>Détecte et applique les mises à jour de logiciels via le gestionnaire de paquets Windows (winget).</summary>
public interface ISoftwareUpdater
{
    Task<SoftwareUpdateScan> ScanAsync(CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(string packageId, CancellationToken cancellationToken = default);
}
