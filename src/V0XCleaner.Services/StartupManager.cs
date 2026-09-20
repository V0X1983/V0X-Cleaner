using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.Startup;

namespace V0XCleaner.Services;

/// <summary>Agrège les différentes sources de démarrage (registre, dossier, tâches planifiées).</summary>
public sealed class StartupManager : IStartupManager
{
    private readonly IReadOnlyList<IStartupProvider> _providers =
    [
        new RegistryRunStartupProvider(),
        new StartupFolderStartupProvider(),
        new ScheduledTaskStartupProvider()
    ];

    public async Task<IReadOnlyList<StartupEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        var entries = new List<StartupEntry>();
        foreach (var provider in _providers)
        {
            entries.AddRange(await provider.GetEntriesAsync(cancellationToken));
        }

        return entries;
    }

    public Task<bool> SetEnabledAsync(StartupEntry entry, bool enabled, CancellationToken cancellationToken = default) =>
        GetProvider(entry.Source).SetEnabledAsync(entry, enabled, cancellationToken);

    public Task<bool> DeleteAsync(StartupEntry entry, CancellationToken cancellationToken = default) =>
        GetProvider(entry.Source).DeleteAsync(entry, cancellationToken);

    private IStartupProvider GetProvider(StartupEntrySource source) =>
        _providers.First(p => p.Source == source);
}
