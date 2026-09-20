using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.Services;

/// <summary>
/// Calcule un score de santé global en réutilisant directement les catalogues des autres modules
/// (Nettoyeur, Registre, Démarrage) plutôt que de dupliquer leur logique de détection.
/// </summary>
public sealed class HealthCheckService(
    ICleaningCatalog cleaningCatalog,
    IRegistryIssueCatalog registryIssueCatalog,
    IStartupManager startupManager) : IHealthCheckService
{
    public async Task<HealthCheckResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<HealthCheckItem>
        {
            CheckDiskSpace(),
            await CheckJunkAsync(cancellationToken),
            await CheckRegistryAsync(cancellationToken),
            await CheckStartupAsync(cancellationToken)
        };

        var overallScore = items.Count > 0 ? (int)items.Average(i => i.Score) : 100;

        return new HealthCheckResult { OverallScore = overallScore, Items = items };
    }

    private static HealthCheckItem CheckDiskSpace()
    {
        try
        {
            var systemDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
            var drive = new DriveInfo(systemDrive);
            var freePercent = drive.TotalSize > 0 ? drive.AvailableFreeSpace * 100.0 / drive.TotalSize : 100;
            var score = Math.Clamp((int)(freePercent / 20.0 * 100), 0, 100);

            return new HealthCheckItem
            {
                Title = "Espace disque",
                Description = $"{freePercent:0.#} % d'espace libre sur {systemDrive} ({FormatGb(drive.AvailableFreeSpace)} / {FormatGb(drive.TotalSize)}).",
                Score = score,
                Severity = ToSeverity(score),
                Recommendation = score < 80 ? "Consultez le Nettoyeur ou l'Analyseur de disque pour libérer de l'espace." : "Rien à signaler."
            };
        }
        catch (Exception ex) when (ex is IOException or ArgumentException)
        {
            return new HealthCheckItem
            {
                Title = "Espace disque",
                Description = "Impossible de lire les informations du disque système.",
                Score = 100,
                Severity = HealthCheckSeverity.Good,
                Recommendation = "Rien à signaler."
            };
        }
    }

    private async Task<HealthCheckItem> CheckJunkAsync(CancellationToken cancellationToken)
    {
        long total = 0;
        foreach (var task in cleaningCatalog.GetTasks().Where(t => t.SelectedByDefault && t.Section == CleaningSection.System))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var result = await task.Scanner.ScanAsync(cancellationToken);
                total += result.TotalSizeBytes;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }

        var megabytes = total / (1024.0 * 1024.0);
        var score = Math.Clamp(100 - (int)(megabytes / 100) * 10, 0, 100);

        return new HealthCheckItem
        {
            Title = "Fichiers temporaires",
            Description = $"{FormatBytes(total)} de fichiers récupérables détectés (catégories système).",
            Score = score,
            Severity = ToSeverity(score),
            Recommendation = score < 80 ? "Ouvrez l'onglet Nettoyeur pour analyser et nettoyer." : "Rien à signaler."
        };
    }

    private async Task<HealthCheckItem> CheckRegistryAsync(CancellationToken cancellationToken)
    {
        var count = 0;
        foreach (var task in registryIssueCatalog.GetTasks())
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var result = await task.Scanner.ScanAsync(cancellationToken);
                count += result.Items.Count;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }

        var score = Math.Clamp(100 - count / 3, 0, 100);

        return new HealthCheckItem
        {
            Title = "Registre",
            Description = $"{count} problème(s) de registre détecté(s).",
            Score = score,
            Severity = ToSeverity(score),
            Recommendation = score < 80 ? "Ouvrez l'onglet Registre pour analyser et réparer." : "Rien à signaler."
        };
    }

    private async Task<HealthCheckItem> CheckStartupAsync(CancellationToken cancellationToken)
    {
        var entries = await startupManager.GetEntriesAsync(cancellationToken);
        var enabledCount = entries.Count(e => e.IsEnabled);
        var score = Math.Clamp(100 - Math.Max(0, enabledCount - 5) * 10, 0, 100);

        return new HealthCheckItem
        {
            Title = "Démarrage",
            Description = $"{enabledCount} programme(s) actif(s) au démarrage.",
            Score = score,
            Severity = ToSeverity(score),
            Recommendation = score < 80 ? "Ouvrez Outils > Démarrage pour désactiver les entrées superflues." : "Rien à signaler."
        };
    }

    private static HealthCheckSeverity ToSeverity(int score) => score switch
    {
        >= 80 => HealthCheckSeverity.Good,
        >= 50 => HealthCheckSeverity.Warning,
        _ => HealthCheckSeverity.Critical
    };

    private static string FormatGb(long bytes) => $"{bytes / (1024.0 * 1024 * 1024):0.#} Go";

    private static string FormatBytes(long bytes)
    {
        string[] units = ["o", "Ko", "Mo", "Go", "To"];
        if (bytes <= 0)
        {
            return "0 o";
        }

        double value = bytes;
        var unitIndex = 0;
        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0 ? $"{value:0} {units[unitIndex]}" : $"{value:0.##} {units[unitIndex]}";
    }
}
