using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.Native;

namespace V0XCleaner.Services.Cleaners;

/// <summary>Vide le presse-papiers Windows.</summary>
public sealed class ClipboardCleaner(ILogger<ClipboardCleaner> logger) : ICleaner
{
    public string Key => "clipboard";

    public Task<CleanupResult> CleanAsync(
        IReadOnlyList<CleanupItem> items,
        OperationMode mode,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
        {
            return Task.FromResult(new CleanupResult { Mode = mode, SucceededCount = 0, FailedCount = 0, FreedBytes = 0, Errors = [] });
        }

        if (mode == OperationMode.Simulate)
        {
            return Task.FromResult(new CleanupResult { Mode = mode, SucceededCount = items.Count, FailedCount = 0, FreedBytes = 0, Errors = [] });
        }

        var ok = User32Interop.TryEmptyClipboard();
        if (ok)
        {
            logger.LogInformation("Presse-papiers vidé.");
            return Task.FromResult(new CleanupResult { Mode = mode, SucceededCount = items.Count, FailedCount = 0, FreedBytes = 0, Errors = [] });
        }

        logger.LogWarning("Échec du vidage du presse-papiers.");
        return Task.FromResult(new CleanupResult
        {
            Mode = mode,
            SucceededCount = 0,
            FailedCount = items.Count,
            FreedBytes = 0,
            Errors = [new CleanupError(items[0].Id, "Impossible de vider le presse-papiers (peut-être verrouillé par une autre application).")]
        });
    }
}
