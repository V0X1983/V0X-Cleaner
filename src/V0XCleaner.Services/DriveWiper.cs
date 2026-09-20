using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.Services;

/// <summary>
/// Remplit l'espace libre d'un lecteur avec un ou plusieurs motifs jusqu'à ce que le disque
/// signale qu'il est plein (IOException), puis supprime le fichier temporaire pour restituer
/// l'espace. Le Gutmann "35 passes" est simplifié en 35 passes de données aléatoires plutôt que
/// de reproduire ses 35 motifs historiques exacts (obsolètes sur tout support autre que les
/// disques magnétiques MFM/RLL des années 1990) : voir la documentation utilisateur.
/// </summary>
public sealed class DriveWiper : IDriveWiper
{
    private const int BufferSize = 4 * 1024 * 1024;

    public async Task WipeFreeSpaceAsync(
        string driveRoot,
        WipeMethod method,
        IProgress<WipeProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var patterns = GetPatterns(method);
        var driveInfo = new DriveInfo(driveRoot);
        var totalEstimate = driveInfo.AvailableFreeSpace;
        var tempFilePath = Path.Combine(driveRoot, $"v0xcleaner-wipe-{Guid.NewGuid():N}.tmp");

        try
        {
            for (var passIndex = 0; passIndex < patterns.Count; passIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await WipePassAsync(tempFilePath, patterns[passIndex], passIndex + 1, patterns.Count, totalEstimate, progress, cancellationToken);
            }
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch (IOException)
                {
                    // Sera nettoyé manuellement si nécessaire ; ne doit pas masquer une éventuelle
                    // erreur d'origine de l'appelant.
                }
            }
        }
    }

    private static async Task WipePassAsync(
        string filePath,
        byte? fixedPattern,
        int passNumber,
        int totalPasses,
        long totalEstimate,
        IProgress<WipeProgress>? progress,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[BufferSize];
        if (fixedPattern.HasValue)
        {
            Array.Fill(buffer, fixedPattern.Value);
        }

        long written = 0;

        await using var stream = new FileStream(
            filePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.WriteThrough);

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!fixedPattern.HasValue)
                {
                    Random.Shared.NextBytes(buffer);
                }

                await stream.WriteAsync(buffer, cancellationToken);
                written += buffer.Length;
                progress?.Report(new WipeProgress(written, totalEstimate, passNumber, totalPasses));
            }
        }
        catch (IOException)
        {
            // Disque plein : fin normale de cette passe.
        }
    }

    private static IReadOnlyList<byte?> GetPatterns(WipeMethod method) => method switch
    {
        WipeMethod.SinglePassZero => [(byte)0x00],
        WipeMethod.Dod3Pass => [(byte)0x00, (byte)0xFF, null],
        WipeMethod.Gutmann35Pass => Enumerable.Repeat((byte?)null, 35).ToList(),
        _ => [(byte)0x00]
    };
}
