using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.RegistryCleanup;

namespace V0XCleaner.Services.Cleaners;

/// <summary>
/// Supprime une valeur unique à l'intérieur d'une clé conservée (DLL partagée orpheline, police
/// invalide, entrée MuiCache...). Id attendu au format "HKxx\Chemin\Clé||NomDeLaValeur"
/// (le séparateur "||" évite toute ambiguïté quand le nom de la valeur contient lui-même des
/// antislashs, ex: un chemin de fichier complet). Sauvegarde .reg obligatoire avant suppression.
/// </summary>
public sealed class RegistryValueDeletionCleaner(IRegistryBackupService backupService, ILogger<RegistryValueDeletionCleaner> logger) : ICleaner
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
                logger.LogWarning("Échec suppression valeur {Id} : {Error}", item.Id, ex.Message);
                errors.Add(new CleanupError(item.Id, ex.Message));
            }
        }

        return new CleanupResult { Mode = mode, SucceededCount = succeeded, FailedCount = errors.Count, FreedBytes = 0, Errors = errors };
    }

    private static (string KeyId, string ValueName) SplitValueId(string id)
    {
        var idx = id.IndexOf("||", StringComparison.Ordinal);
        return idx < 0 ? (id, string.Empty) : (id[..idx], id[(idx + 2)..]);
    }

    private static CleanupResult Empty(OperationMode mode) =>
        new() { Mode = mode, SucceededCount = 0, FailedCount = 0, FreedBytes = 0, Errors = [] };
}
