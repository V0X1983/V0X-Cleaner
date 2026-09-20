using V0XCleaner.Core.Models;

namespace V0XCleaner.Core.Abstractions;

/// <summary>Liste les processus utilisateur les plus gourmands en mémoire (hors processus système protégés) et permet de les fermer.</summary>
public interface IProcessOptimizer
{
    IReadOnlyList<RunningProcessInfo> GetTopProcesses(int count);

    /// <summary>Ferme proprement (fenêtre) si possible, sinon termine le processus. Refuse les processus protégés.</summary>
    bool TryClose(int processId, out string? error);

    /// <summary>Met un processus en veille (gel de tous ses threads) sans le fermer ; il reste en mémoire jusqu'à <see cref="TryResume"/>. Refuse les processus protégés.</summary>
    bool TrySuspend(int processId, out string? error);

    bool TryResume(int processId, out string? error);

    /// <summary>Processus mis en veille par l'application et toujours en cours d'exécution.</summary>
    IReadOnlyList<RunningProcessInfo> GetSleepingProcesses();

    /// <summary>Réactive tous les processus mis en veille (appelé à la fermeture de l'application pour ne rien laisser gelé).</summary>
    void ResumeAll();
}
