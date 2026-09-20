using V0XCleaner.Core.Models;

namespace V0XCleaner.Core.Abstractions;

/// <summary>Liste les services Windows tiers et change leur type de démarrage (les services Microsoft sont exclus).</summary>
public interface IWindowsServiceManager
{
    Task<IReadOnlyList<WindowsServiceEntry>> GetServicesAsync(CancellationToken cancellationToken = default);

    /// <summary>Modifie le type de démarrage ; retourne false en cas d'échec (droits administrateur requis).</summary>
    Task<bool> SetStartModeAsync(string serviceName, ServiceStartMode mode, CancellationToken cancellationToken = default);
}
