namespace V0XCleaner.Core.Abstractions;

/// <summary>
/// Sauvegarde automatique et non optionnelle du registre avant toute modification faite par
/// le module Registre, et restauration à la demande de l'utilisateur.
/// </summary>
public interface IRegistryBackupService
{
    /// <summary>
    /// Exporte les clés indiquées (format "HKCU\Chemin\Vers\Clé") vers un fichier .reg unique
    /// et horodaté. Retourne le chemin du fichier créé, ou null si aucune clé n'a pu être exportée
    /// (dans ce cas, l'appelant doit renoncer à toute modification par précaution).
    /// </summary>
    Task<string?> BackupKeysAsync(IReadOnlyCollection<string> registryKeyPaths, CancellationToken cancellationToken = default);

    /// <summary>Liste les fichiers de sauvegarde disponibles, du plus récent au plus ancien.</summary>
    IReadOnlyList<string> ListBackups();

    /// <summary>Réimporte un fichier .reg précédemment créé par <see cref="BackupKeysAsync"/>.</summary>
    Task<bool> RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default);
}
