using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.Scanners;

namespace V0XCleaner.Services.Cleaners;

/// <summary>Supprime les valeurs RunMRU / TypedPaths identifiées par <see cref="RegistryMruScanner"/>.</summary>
public sealed class RegistryMruCleaner(ILogger<RegistryMruCleaner> logger) : ICleaner
{
    public string Key => "registry-mru";

    public Task<CleanupResult> CleanAsync(
        IReadOnlyList<CleanupItem> items,
        OperationMode mode,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<CleanupError>();
        var succeeded = 0;

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Id format: HKCU\<subKeyPath>\<valueName>
            var parts = item.Id.Split('\\');
            var valueName = parts[^1];
            var subKeyPath = string.Join('\\', parts[1..^1]);

            if (mode == OperationMode.Simulate)
            {
                succeeded++;
                continue;
            }

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(subKeyPath, writable: true);
                key?.DeleteValue(valueName, throwOnMissingValue: false);
                logger.LogInformation("Valeur de registre supprimée : {Id}", item.Id);
                succeeded++;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
            {
                logger.LogWarning("Échec suppression registre {Id} : {Error}", item.Id, ex.Message);
                errors.Add(new CleanupError(item.Id, ex.Message));
            }
        }

        return Task.FromResult(new CleanupResult
        {
            Mode = mode,
            SucceededCount = succeeded,
            FailedCount = errors.Count,
            FreedBytes = 0,
            Errors = errors
        });
    }
}
