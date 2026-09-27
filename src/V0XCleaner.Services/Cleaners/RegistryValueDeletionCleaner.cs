using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Core.Models.Elevation;
using V0XCleaner.Services.RegistryCleanup;

namespace V0XCleaner.Services.Cleaners;

/// <summary>
/// Supprime une valeur unique à l'intérieur d'une clé conservée (DLL partagée orpheline, police
/// invalide, entrée MuiCache...). Id attendu au format "HKxx\Chemin\Clé||NomDeLaValeur"
/// (le séparateur "||" évite toute ambiguïté quand le nom de la valeur contient lui-même des
/// antislashs, ex: un chemin de fichier complet). Sauvegarde .reg obligatoire avant suppression.
///
/// Les valeurs qui échouent en direct par manque de droits sont retentées en une seule fois via
/// <see cref="IElevatedOperationClient"/> (une seule invite UAC pour tout le lot) — voir
/// RegistryKeyDeletionCleaner pour le même principe.
/// </summary>
public sealed class RegistryValueDeletionCleaner(
    IRegistryBackupService backupService,
    IElevatedOperationClient elevatedClient,
    ILogger<RegistryValueDeletionCleaner> logger) : ICleaner
{
    public string Key => "registry-value-deletion";

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

        var keyPaths = items.Select(i => SplitValueId(i.Id).KeyId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var backupPath = await backupService.BackupKeysAsync(keyPaths, cancellationToken);
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

            var (keyId, valueName) = SplitValueId(item.Id);
            var (hivePrefix, subKeyPath) = RegistryPathHelper.SplitKeyId(keyId);

            try
            {
                using var baseKey = RegistryPathHelper.GetBaseKey(hivePrefix);
                using var subKey = baseKey.OpenSubKey(subKeyPath, writable: true);
                subKey?.DeleteValue(valueName, throwOnMissingValue: false);
                succeeded++;
                logger.LogInformation("Valeur de registre supprimée : {Id}", item.Id);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
            {
                deniedItems.Add(item);
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
                new CleanupError(i.Id, "Droits administrateur requis pour supprimer cette valeur.")));
        }

        return new CleanupResult { Mode = mode, SucceededCount = succeeded, FailedCount = errors.Count, FreedBytes = 0, Errors = errors };
    }

    private async Task<(int Succeeded, List<CleanupError> Errors)> RetryElevatedAsync(
        List<CleanupItem> deniedItems, CancellationToken cancellationToken)
    {
        var operations = deniedItems.Select(item =>
        {
            var (keyId, valueName) = SplitValueId(item.Id);
            var (hivePrefix, subKeyPath) = RegistryPathHelper.SplitKeyId(keyId);
            return new ElevatedRegistryOperation(item.Id, ElevatedRegistryOperationKind.DeleteValue, hivePrefix, subKeyPath, valueName);
        }).ToList();

        var response = await elevatedClient.ExecuteAsync(operations, cancellationToken);
        var errors = new List<CleanupError>();

        if (!response.Success && response.Results.Count == 0)
        {
            logger.LogWarning("Retry élevé impossible pour {Count} valeur(s) : {Error}", deniedItems.Count, response.ErrorMessage);
            return (0, deniedItems.Select(i => new CleanupError(i.Id, response.ErrorMessage ?? "Élévation impossible.")).ToList());
        }

        var succeeded = 0;
        foreach (var result in response.Results)
        {
            if (result.Success)
            {
                succeeded++;
                logger.LogInformation("Valeur de registre supprimée (élevée) : {Id}", result.OperationId);
            }
            else
            {
                errors.Add(new CleanupError(result.OperationId, result.ErrorMessage ?? "Échec de la suppression élevée."));
            }
        }

        return (succeeded, errors);
    }

    private static (string KeyId, string ValueName) SplitValueId(string id)
    {
        var idx = id.IndexOf("||", StringComparison.Ordinal);
        return idx < 0 ? (id, string.Empty) : (id[..idx], id[(idx + 2)..]);
    }

    private static CleanupResult Empty(OperationMode mode) =>
        new() { Mode = mode, SucceededCount = 0, FailedCount = 0, FreedBytes = 0, Errors = [] };
}
