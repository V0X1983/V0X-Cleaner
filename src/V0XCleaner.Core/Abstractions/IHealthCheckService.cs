using V0XCleaner.Core.Models;

namespace V0XCleaner.Core.Abstractions;

/// <summary>Calcule un score de santé global à partir des catalogues déjà existants (nettoyage, registre, démarrage, disque).</summary>
public interface IHealthCheckService
{
    Task<HealthCheckResult> RunAsync(CancellationToken cancellationToken = default);
}
