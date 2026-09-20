using V0XCleaner.Core.Models;

namespace V0XCleaner.Core.Abstractions;

/// <summary>
/// "Libère la RAM" en réduisant le working set des processus accessibles (EmptyWorkingSet).
/// Ne libère pas de mémoire réellement committée, seulement des pages physiques que Windows peut
/// réutiliser immédiatement : opération sûre et sans effet de bord sur les applications.
/// </summary>
public interface IMemoryOptimizer
{
    Task<MemoryOptimizationResult> FreeMemoryAsync(CancellationToken cancellationToken = default);
}
