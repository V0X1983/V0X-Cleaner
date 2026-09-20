using V0XCleaner.Core.Models;

namespace V0XCleaner.Core.Abstractions;

/// <summary>Pilotes installés et mises à jour de pilotes proposées par Windows Update (pilotes signés uniquement).</summary>
public interface IDriverCatalog
{
    Task<IReadOnlyList<DriverInfo>> GetDriversAsync(CancellationToken cancellationToken = default);

    /// <summary>Interroge Windows Update (peut prendre une minute). Retourne une liste vide si le service est indisponible.</summary>
    Task<IReadOnlyList<DriverUpdateInfo>> GetAvailableUpdatesAsync(CancellationToken cancellationToken = default);

    /// <summary>Télécharge et installe les mises à jour indiquées. Nécessite les droits administrateur. Retourne un message d'erreur, ou null en cas de succès.</summary>
    Task<string?> InstallUpdatesAsync(IReadOnlyCollection<string> updateIds, CancellationToken cancellationToken = default);
}
