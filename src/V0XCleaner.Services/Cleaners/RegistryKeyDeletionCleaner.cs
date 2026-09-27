using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Core.Models.Elevation;
using V0XCleaner.Services.RegistryCleanup;

namespace V0XCleaner.Services.Cleaners;

/// <summary>
/// Supprime des clés de registre entières (extensions orphelines, CLSID cassés, désinstalleurs
/// fantômes...). Une sauvegarde .reg de TOUTES les clés concernées est créée avant la moindre
/// suppression ; si elle échoue, rien n'est supprimé.
///
/// Les clés qui échouent en direct par manque de droits (typiquement HKLM/HKCR sans élévation)
/// sont retentées en une seule fois via <see cref="IElevatedOperationClient"/> (une seule invite
/// UAC pour tout le lot). Avec l'implémentation par défaut (NullElevatedOperationClient, non
/// disponible), ce retry est un no-op silencieux : comportement inchangé pour l'app WPF.
/// </summary>
public sealed class RegistryKeyDeletionCleaner(
    IRegistryBackupService backupService,
    IElevatedOperationClient elevatedClient,
    ILogger<RegistryKeyDeletionCleaner> logger) : ICleaner
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
        var deniedItems = new List<CleanupItem>();
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
            catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
            {
                deniedItems.Add(item);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning("Échec suppression clé {Id} : {Error}", item.Id, ex.Message);
                errors.Add(new CleanupError(item.Id, ex.Message));
            }
        }

        if (deniedItems.Count > 0 && elevatedClient.IsSupported)
        {
            var (elevatedSucceeded, elevatedErrors) = await RetryElevatedAsync(deniedItems, cancellationToken);
            succeeded += elevatedSucceeded;
            errors.AddRange(elevatedErrors);
        }
        else
        {
            errors.AddRange(deniedItems.Select(i =>
                new CleanupError(i.Id, "Droits administrateur requis pour supprimer cette clé.")));
        }

        return new CleanupResult { Mode = mode, SucceededCount = succeeded, FailedCount = errors.Count, FreedBytes = 0, Errors = errors };
    }

    private async Task<(int Succeeded, List<CleanupError> Errors)> RetryElevatedAsync(
        List<CleanupItem> deniedItems, CancellationToken cancellationToken)
    {
        var operations = deniedItems.Select(item =>
        {
            var (hivePrefix, subKeyPath) = RegistryPathHelper.SplitKeyId(item.Id);
            var (parentPath, leafName) = RegistryPathHelper.SplitParent(subKeyPath);
            return new ElevatedRegistryOperation(item.Id, ElevatedRegistryOperationKind.DeleteKey, hivePrefix, parentPath, leafName);
        }).ToList();

        var response = await elevatedClient.ExecuteAsync(operations, cancellationToken);
        var errors = new List<CleanupError>();

        if (!response.Success && response.Results.Count == 0)
        {
            // Le helper n'a pas pu s'exécuter du tout (UAC refusée, helper introuvable...) : tout le lot échoue.
            logger.LogWarning("Retry élevé impossible pour {Count} clé(s) : {Error}", deniedItems.Count, response.ErrorMessage);
            return (0, deniedItems.Select(i => new CleanupError(i.Id, response.ErrorMessage ?? "Élévation impossible.")).ToList());
        }

        var succeeded = 0;
        foreach (var result in response.Results)
        {
            if (result.Success)
            {
                succeeded++;
                logger.LogInformation("Clé de registre supprimée (élevée) : {Id}", result.OperationId);
            }
            else
            {
                errors.Add(new CleanupError(result.OperationId, result.ErrorMessage ?? "Échec de la suppression élevée."));
            }
        }

        return (succeeded, errors);
    }

    private static CleanupResult Empty(OperationMode mode) =>
        new() { Mode = mode, SucceededCount = 0, FailedCount = 0, FreedBytes = 0, Errors = [] };
}
