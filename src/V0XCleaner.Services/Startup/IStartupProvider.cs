using V0XCleaner.Core.Models;

namespace V0XCleaner.Services.Startup;

/// <summary>Une source d'entrées de démarrage (registre, dossier Démarrage, tâches planifiées...).</summary>
internal interface IStartupProvider
{
    StartupEntrySource Source { get; }

    Task<IReadOnlyList<StartupEntry>> GetEntriesAsync(CancellationToken cancellationToken);

    Task<bool> SetEnabledAsync(StartupEntry entry, bool enabled, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(StartupEntry entry, CancellationToken cancellationToken);
}
