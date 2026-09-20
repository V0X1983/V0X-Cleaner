namespace V0XCleaner.Core.Abstractions;

/// <summary>Crée/retire la tâche planifiée Windows qui exécute V0X Cleaner en mode silencieux (--silent --clean).</summary>
public interface IAutoCleanScheduler
{
    Task<bool> ConfigureAsync(bool enabled, string frequency, int hour, CancellationToken cancellationToken = default);

    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);
}
