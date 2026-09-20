using System.Diagnostics;
using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.Services.Cleaners;

/// <summary>Vide le cache de résolution DNS via "ipconfig /flushdns".</summary>
public sealed class DnsCacheCleaner(ILogger<DnsCacheCleaner> logger) : ICleaner
{
    public string Key => "dns-cache";

    public async Task<CleanupResult> CleanAsync(
        IReadOnlyList<CleanupItem> items,
        OperationMode mode,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
        {
            return new CleanupResult { Mode = mode, SucceededCount = 0, FailedCount = 0, FreedBytes = 0, Errors = [] };
        }

        if (mode == OperationMode.Simulate)
        {
            return new CleanupResult { Mode = mode, SucceededCount = items.Count, FailedCount = 0, FreedBytes = 0, Errors = [] };
        }

        try
        {
            var startInfo = new ProcessStartInfo("ipconfig", "/flushdns")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                throw new InvalidOperationException("Impossible de démarrer ipconfig.");
            }

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
            {
                logger.LogInformation("Cache DNS vidé.");
                return new CleanupResult { Mode = mode, SucceededCount = items.Count, FailedCount = 0, FreedBytes = 0, Errors = [] };
            }

            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
            logger.LogWarning("ipconfig /flushdns a retourné le code {Code} : {Error}", process.ExitCode, stderr);
            return new CleanupResult
            {
                Mode = mode,
                SucceededCount = 0,
                FailedCount = items.Count,
                FreedBytes = 0,
                Errors = [new CleanupError(items[0].Id, $"ipconfig /flushdns a échoué (code {process.ExitCode}).")]
            };
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            logger.LogWarning("Échec du vidage du cache DNS : {Error}", ex.Message);
            return new CleanupResult
            {
                Mode = mode,
                SucceededCount = 0,
                FailedCount = items.Count,
                FreedBytes = 0,
                Errors = [new CleanupError(items[0].Id, ex.Message)]
            };
        }
    }
}
