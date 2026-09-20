using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.FileSystem;

namespace V0XCleaner.Services.Cleaners;

/// <summary>
/// Nettoyeur générique pour tout CleanupItem dont le DisplayPath désigne un fichier réel
/// (cas de la grande majorité des scanners, via PathPatternScanner). Chaque suppression
/// repasse par IPathGuard et est journalisée individuellement.
/// </summary>
public sealed class FileDeletionCleaner(IPathGuard pathGuard, ILogger<FileDeletionCleaner> logger) : ICleaner
{
    public string Key => "file-deletion";

    public Task<CleanupResult> CleanAsync(
        IReadOnlyList<CleanupItem> items,
        OperationMode mode,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<CleanupError>();
        var succeeded = 0;
        long freedBytes = 0;

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!pathGuard.IsSafeToDelete(item.DisplayPath, out var guardReason))
            {
                logger.LogWarning("Suppression refusée par PathGuard : {Path} ({Reason})", item.DisplayPath, guardReason);
                errors.Add(new CleanupError(item.Id, guardReason ?? "Chemin refusé par le garde-fou de sécurité."));
                continue;
            }

            if (mode == OperationMode.Simulate)
            {
                succeeded++;
                freedBytes += item.SizeBytes;
                continue;
            }

            if (FileSystemHelpers.TryDeleteFile(item.DisplayPath, out var error))
            {
                logger.LogInformation("Fichier supprimé : {Path} ({Size} octets)", item.DisplayPath, item.SizeBytes);
                succeeded++;
                freedBytes += item.SizeBytes;
            }
            else
            {
                logger.LogWarning("Échec de suppression : {Path} — {Error}", item.DisplayPath, error);
                errors.Add(new CleanupError(item.Id, error ?? "Erreur inconnue."));
            }
        }

        return Task.FromResult(new CleanupResult
        {
            Mode = mode,
            SucceededCount = succeeded,
            FailedCount = errors.Count,
            FreedBytes = freedBytes,
            Errors = errors
        });
    }
}
