using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Services.Native;

namespace V0XCleaner.Services.RegistryCleanup;

/// <summary>
/// Sauvegarde/restauration du registre via reg.exe export/import. Chaque nettoyage du module
/// Registre commence obligatoirement par un appel à <see cref="BackupKeysAsync"/> : si la
/// sauvegarde échoue, aucune suppression n'est effectuée (voir RegistryKeyDeletionCleaner /
/// RegistryValueDeletionCleaner).
/// </summary>
public sealed class RegistryBackupService(ILogger<RegistryBackupService> logger) : IRegistryBackupService
{
    /// <summary>Nombre de sauvegardes conservées ; les plus anciennes sont supprimées à chaque nouvelle sauvegarde.</summary>
    private const int MaxBackups = 10;

    public async Task<string?> BackupKeysAsync(IReadOnlyCollection<string> registryKeyPaths, CancellationToken cancellationToken = default)
    {
        var uniqueKeys = registryKeyPaths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (uniqueKeys.Count == 0)
        {
            return null;
        }

        var sections = new List<string>();

        foreach (var keyPath in uniqueKeys)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tempFile = Path.Combine(Path.GetTempPath(), $"v0xcleaner-regexport-{Guid.NewGuid()}.reg");
            try
            {
                if (await RunRegExeAsync($"export \"{keyPath}\" \"{tempFile}\" /y", cancellationToken) && File.Exists(tempFile))
                {
                    var content = await File.ReadAllTextAsync(tempFile, Encoding.Unicode, cancellationToken);
                    var section = ExtractSection(content);
                    if (!string.IsNullOrWhiteSpace(section))
                    {
                        sections.Add(section);
                    }
                }
                else
                {
                    logger.LogWarning("Impossible d'exporter la clé {Key} pour la sauvegarde.", keyPath);
                }
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        if (sections.Count == 0)
        {
            logger.LogWarning("Aucune clé n'a pu être sauvegardée : nettoyage annulé par précaution.");
            return null;
        }

        Directory.CreateDirectory(AppPaths.RegistryBackupsFolder);
        var backupFilePath = Path.Combine(AppPaths.RegistryBackupsFolder, $"backup-{DateTime.Now:yyyyMMdd-HHmmss}.reg");
        var combined = "Windows Registry Editor Version 5.00\r\n\r\n" + string.Join("\r\n\r\n", sections) + "\r\n";
        await File.WriteAllTextAsync(backupFilePath, combined, Encoding.Unicode, cancellationToken);

        logger.LogInformation("Sauvegarde registre créée : {Path} ({Count} clé(s)).", backupFilePath, sections.Count);
        PruneOldBackups();
        return backupFilePath;
    }

    private void PruneOldBackups()
    {
        try
        {
            // Les noms contiennent la date : le tri par nom décroissant place les plus récentes en premier.
            foreach (var old in ListBackups().Skip(MaxBackups))
            {
                File.Delete(old);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning("Impossible de purger les anciennes sauvegardes du registre : {Error}", ex.Message);
        }
    }

    public IReadOnlyList<string> ListBackups()
    {
        if (!Directory.Exists(AppPaths.RegistryBackupsFolder))
        {
            return [];
        }

        return Directory.GetFiles(AppPaths.RegistryBackupsFolder, "*.reg")
            .OrderByDescending(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<bool> RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(backupFilePath))
        {
            return false;
        }

        var ok = await RunRegExeAsync($"import \"{backupFilePath}\"", cancellationToken);
        if (ok)
        {
            logger.LogInformation("Sauvegarde registre restaurée : {Path}", backupFilePath);
        }
        else
        {
            logger.LogWarning("Échec de la restauration de la sauvegarde : {Path}", backupFilePath);
        }

        return ok;
    }

    private static string ExtractSection(string exportedContent)
    {
        var idx = exportedContent.IndexOf('[');
        return idx >= 0 ? exportedContent[idx..].TrimEnd() : string.Empty;
    }

    private async Task<bool> RunRegExeAsync(string arguments, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo("reg.exe", arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return false;
            }

            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            logger.LogWarning("Échec d'exécution de reg.exe {Arguments} : {Error}", arguments, ex.Message);
            return false;
        }
    }
}
