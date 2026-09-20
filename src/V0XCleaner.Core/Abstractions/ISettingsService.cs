using V0XCleaner.Core.Models;

namespace V0XCleaner.Core.Abstractions;

public interface ISettingsService
{
    AppSettings Current { get; }

    Task SaveAsync(CancellationToken cancellationToken = default);

    event EventHandler? SettingsChanged;
}
