using V0XCleaner.Core.Models;

namespace V0XCleaner.Core.Abstractions;

/// <summary>
/// Point d'entrée unique pour lister et piloter les programmes de démarrage, quelle que soit
/// leur origine (registre Run/RunOnce, dossier Démarrage, tâche planifiée au logon).
/// </summary>
public interface IStartupManager
{
    Task<IReadOnlyList<StartupEntry>> GetEntriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Active/désactive l'entrée sans la supprimer (comme le Gestionnaire des tâches Windows).</summary>
    Task<bool> SetEnabledAsync(StartupEntry entry, bool enabled, CancellationToken cancellationToken = default);

    /// <summary>Supprime définitivement l'entrée (valeur de registre, raccourci, ou tâche planifiée).</summary>
    Task<bool> DeleteAsync(StartupEntry entry, CancellationToken cancellationToken = default);
}
