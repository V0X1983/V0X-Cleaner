using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.Native;

namespace V0XCleaner.Services.Cleaners;

/// <summary>Vide la Corbeille Windows via l'API Shell (SHEmptyRecycleBin).</summary>
public sealed class RecycleBinCleaner(ILogger<RecycleBinCleaner> logger) : ICleaner
{
    public string Key => "recycle-bin";

    public Task<CleanupResult> CleanAsync(
        IReadOnlyList<CleanupItem> items,
        OperationMode mode,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
        {
            return Task.FromResult(new CleanupResult
            {
                Mode = mode,
                SucceededCount = 0,
                FailedCount = 0,
                FreedBytes = 0,
                Errors = []
            });
        }

        var totalBytes = items.Sum(i => i.SizeBytes);

        if (mode == OperationMode.Simulate)
        {
            return Task.FromResult(new CleanupResult
            {
                Mode = mode,
                SucceededCount = items.Count,
                FailedCount = 0,
                FreedBytes = totalBytes,
                Errors = []
            });
        }

        var ok = Shell32Interop.EmptyRecycleBin();
        if (ok)
        {
            logger.LogInformation("Corbeille vidée ({Size} octets libérés).", totalBytes);
            return Task.FromResult(new CleanupResult
            {
                Mode = mode,
                SucceededCount = items.Count,
                FailedCount = 0,
                FreedBytes = totalBytes,
                Errors = []
            });
        }

        logger.LogWarning("Échec du vidage de la Corbeille.");
        return Task.FromResult(new CleanupResult
        {
            Mode = mode,
            SucceededCount = 0,
            FailedCount = items.Count,
            FreedBytes = 0,
            Errors = [new CleanupError(items[0].Id, "Impossible de vider la Corbeille (SHEmptyRecycleBin a échoué).")]
        });
    }
}
