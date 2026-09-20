using V0XCleaner.Core.Models;

namespace V0XCleaner.Core.Abstractions;

/// <summary>Liste les processus utilisateur les plus gourmands en mémoire (hors processus système protégés) et permet de les fermer.</summary>
public interface IProcessOptimizer
{
    IReadOnlyList<RunningProcessInfo> GetTopProcesses(int count);

    /// <summary>Ferme proprement (fenêtre) si possible, sinon termine le processus. Refuse les processus protégés.</summary>
    bool TryClose(int processId, out string? error);
}
