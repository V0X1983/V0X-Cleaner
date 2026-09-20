using System.ComponentModel;
using System.Diagnostics;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.Native;

namespace V0XCleaner.Services;

/// <summary>
/// Réduit le working set des processus accessibles pour rendre de la mémoire physique disponible
/// à Windows immédiatement, sans fermer ni perturber aucune application. Les processus système ou
/// d'un autre utilisateur, inaccessibles sans élévation, sont simplement ignorés.
/// </summary>
public sealed class MemoryOptimizer : IMemoryOptimizer
{
    public Task<MemoryOptimizationResult> FreeMemoryAsync(CancellationToken cancellationToken = default)
    {
        var trimmed = 0;
        long freed = 0;

        foreach (var process in Process.GetProcesses())
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var before = process.WorkingSet64;
                if (PsApiInterop.EmptyWorkingSet(process.Handle))
                {
                    process.Refresh();
                    var after = process.WorkingSet64;
                    if (after < before)
                    {
                        freed += before - after;
                        trimmed++;
                    }
                }
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or NotSupportedException)
            {
                // Processus système ou protégé inaccessible sans élévation : ignoré.
            }
            finally
            {
                process.Dispose();
            }
        }

        return Task.FromResult(new MemoryOptimizationResult(trimmed, freed));
    }
}
