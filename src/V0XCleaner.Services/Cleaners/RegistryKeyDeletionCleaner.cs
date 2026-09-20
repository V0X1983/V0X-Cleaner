using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.RegistryCleanup;

namespace V0XCleaner.Services.Cleaners;

/// <summary>
/// Supprime des clés de registre entières (extensions orphelines, CLSID cassés, désinstalleurs
/// fantômes...). Une sauvegarde .reg de TOUTES les clés concernées est créée avant la moindre
/// suppression ; si elle échoue, rien n'est supprimé.
/// </summary>
public sealed class RegistryKeyDeletionCleaner(IRegistryBackupService backupService, ILogger<RegistryKeyDeletionCleaner> logger) : ICleaner
{
    public string Key => "registry-key-deletion";

    public async Task<CleanupResult> CleanAsync(
        IReadOnlyList<CleanupItem> items,
        OperationMode mode,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
        {
            return Empty(mode);
        }

        if (mode == OperationMode.Simulate)
        {
            return new CleanupResult { Mode = mode, SucceededCount = items.Count, FailedCount = 0, FreedBytes = 0, Errors = [] };
        }

        var backupPath = await backupService.BackupKeysAsync(items.Select(i => i.Id).ToList(), cancellationToken);
        if (backupPath is null)
        {
            return new CleanupResult
            {
                Mode = mode,
                SucceededCount = 0,
                FailedCount = items.Count,
                FreedBytes = 0,
                Errors = items.Select(i => new CleanupError(i.Id, "Sauvegarde de sécurité impossible : aucune modification effectuée.")).ToList()
            };
        }

        var errors = new List<CleanupError>();
        var succeeded = 0;

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (hivePrefix, subKeyPath) = RegistryPathHelper.SplitKeyId(item.Id);
            var (parentPath, leafName) = RegistryPathHelper.SplitParent(subKeyPath);

            try
            {
                using var baseKey = RegistryPathHelper.GetBaseKey(hivePrefix);
                using var parentKey = parentPath.Length == 0 ? baseKey : baseKey.OpenSubKey(parentPath, writable: true);
                parentKey?.DeleteSubKeyTree(leafName, throwOnMissingSubKey: false);
                succeeded++;
                logger.LogInformation("Clé de registre supprimée : {Id}", item.Id);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or ArgumentException)
            {
                logger.LogWarning("Échec suppression clé {Id} : {Error}", item.Id, ex.Message);
                errors.Add(new CleanupError(item.Id, ex.Message));
            }
        }

        return new CleanupResult { Mode = mode, SucceededCount = succeeded, FailedCount = errors.Count, FreedBytes = 0, Errors = errors };
    }

    private static CleanupResult Empty(OperationMode mode) =>
        new() { Mode = mode, SucceededCount = 0, FailedCount = 0, FreedBytes = 0, Errors = [] };
}
