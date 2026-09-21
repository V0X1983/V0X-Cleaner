using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.Services;

public sealed partial class SoftwareUpdater(ILogger<SoftwareUpdater> logger) : ISoftwareUpdater
{
    public async Task<SoftwareUpdateScan> ScanAsync(CancellationToken cancellationToken = default)
    {
        var (started, exitCode, output) = await RunWingetAsync("upgrade --include-unknown --accept-source-agreements", cancellationToken);
        if (!started)
        {
            return new SoftwareUpdateScan(false, [], "winget est introuvable sur ce PC (installez « Programme d'installation d'applications » depuis le Microsoft Store).");
        }

        var updates = Parse(output);
        return new SoftwareUpdateScan(true, updates, null);
    }

    public async Task<SoftwareUpdateResult> UpdateAsync(string packageId, IProgress<SoftwareUpdateProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (!PackageIdRegex().IsMatch(packageId))
        {
            return new SoftwareUpdateResult(false, "Identifiant de paquet invalide.");
        }

        // winget n'affiche pas de progression quand sa sortie est redirigée : le pourcentage du téléchargement
        // est calculé à part (voir WingetDownloadWatcher) jusqu'à la ligne qui annonce l'installation.
        using var watchCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var startedUtc = DateTime.UtcNow;
        Task? watcher = null;

        var noRestart = await GetNoRestartArgumentAsync(packageId, cancellationToken);

        var (started, exitCode, output) = await RunWingetAsync(
            $"upgrade --id {packageId} --exact --include-unknown --silent --accept-package-agreements --accept-source-agreements{noRestart}",
            cancellationToken,
            line =>
            {
                if (IsInstallLine(line))
                {
                    watchCts.Cancel();
                }
                else if (watcher is null && progress is not null && WingetDownloadWatcher.TryGetUrl(line, out var url))
                {
                    watcher = WingetDownloadWatcher.WatchAsync(packageId, url, startedUtc, progress, watchCts.Token);
                }

                ReportProgress(line, progress);
            });

        watchCts.Cancel();
        if (watcher is not null)
        {
            await watcher;
        }

        logger.LogInformation("Mise à jour winget de {Id} : code {Code}", packageId, exitCode);

        if (!started)
        {
            return new SoftwareUpdateResult(false, "winget est introuvable sur ce PC.");
        }

        if (exitCode == 0)
        {
            return new SoftwareUpdateResult(true);
        }

        return new SoftwareUpdateResult(false, ExplainFailure(output, exitCode));
    }

    public async Task<Uri?> GetWebsiteAsync(string packageId, CancellationToken cancellationToken = default)
    {
        if (!PackageIdRegex().IsMatch(packageId))
        {
            return null;
        }

        var (started, _, output) = await RunWingetAsync(
            $"show --id {packageId} --exact --accept-source-agreements", cancellationToken);
        return started ? ParseWebsite(output) : null;
    }

    internal static Uri? ParseWebsite(string output)
    {
        // Page d'accueil en priorité, puis site de l'éditeur (libellés winget en anglais ou en français).
        string[] labels = ["Homepage", "Page d'accueil", "Publisher Url", "URL de l'éditeur", "Éditeur URL"];
        foreach (var label in labels)
        {
            foreach (var line in output.Replace('’', '\'').Split(['\r', '\n']))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith(label, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var colon = trimmed.IndexOf(':', label.Length);
                if (colon < 0)
                {
                    continue;
                }

                if (Uri.TryCreate(trimmed[(colon + 1)..].Trim(), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
                {
                    return uri;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Les installateurs de type Burn/WiX redémarrent le PC tout seuls en mode silencieux quand ils en ont besoin ;
    /// leur option standard « /norestart » l'empêche. Elle n'est ajoutée que pour ces types, car d'autres
    /// installateurs rejetteraient une option inconnue.
    /// </summary>
    private async Task<string> GetNoRestartArgumentAsync(string packageId, CancellationToken cancellationToken)
    {
        var (started, _, output) = await RunWingetAsync($"show --id {packageId} --exact --accept-source-agreements", cancellationToken);
        var type = started ? ParseInstallerType(output) : null;
        return type is "burn" or "wix" ? " --custom \"/norestart\"" : string.Empty;
    }

    /// <summary>Lit le type de programme d'installation (msi, exe, burn...) dans la sortie de « winget show ».</summary>
    internal static string? ParseInstallerType(string output)
    {
        var match = InstallerTypeRegex().Match(output.Replace('’', '\''));
        return match.Success ? match.Groups[1].Value.ToLowerInvariant() : null;
    }

    /// <summary>Traduit un échec de winget en message utile quand la cause est connue.</summary>
    internal static string ExplainFailure(string output, int exitCode)
    {
        var text = output.Replace('’', '\'');

        // Certains paquets exigent un dossier d'installation : winget le demande en interactif, ce qui est impossible ici.
        if (text.Contains("install location", StringComparison.OrdinalIgnoreCase)
            || text.Contains("emplacement d'installation", StringComparison.OrdinalIgnoreCase))
        {
            return "Dossier d'installation requis : voir le site (globe)";
        }

        return $"Échec de winget (code 0x{exitCode:X8}) : droits administrateur ou application ouverte ?";
    }

    /// <summary>Vrai pour les lignes de winget qui marquent la fin du téléchargement et le début de l'installation (anglais ou français).</summary>
    internal static bool IsInstallLine(string line)
    {
        var text = line.Replace('’', '\'');
        return text.Contains("Starting package install", StringComparison.OrdinalIgnoreCase)
            || text.Contains("verified installer hash", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Démarrage de l'installation", StringComparison.OrdinalIgnoreCase)
            || (text.Contains("hachage", StringComparison.OrdinalIgnoreCase) && text.Contains("vérifié", StringComparison.OrdinalIgnoreCase));
    }

    internal static void ReportProgress(string line, IProgress<SoftwareUpdateProgress>? progress)
    {
        if (progress is null)
        {
            return;
        }

        if (IsInstallLine(line))
        {
            progress.Report(new SoftwareUpdateProgress(SoftwareUpdatePhase.Installing, null));
            return;
        }

        if (line.Contains("Downloading", StringComparison.OrdinalIgnoreCase)
            || line.Contains("Téléchargement", StringComparison.OrdinalIgnoreCase))
        {
            progress.Report(new SoftwareUpdateProgress(SoftwareUpdatePhase.Downloading, null));
            return;
        }

        // Barre de progression winget : « ████▒▒▒  12.3 MB / 45.0 MB » ou « 37% ».
        var sizes = SizeProgressRegex().Match(line);
        if (sizes.Success
            && double.TryParse(sizes.Groups[1].Value.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture, out var done)
            && double.TryParse(sizes.Groups[3].Value.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture, out var total)
            && total > 0)
        {
            progress.Report(new SoftwareUpdateProgress(SoftwareUpdatePhase.Downloading, Math.Clamp(done / total * 100, 0, 100)));
            return;
        }

        var percent = PercentProgressRegex().Match(line);
        if (percent.Success && int.TryParse(percent.Groups[1].Value, out var pct))
        {
            progress.Report(new SoftwareUpdateProgress(SoftwareUpdatePhase.Downloading, Math.Clamp(pct, 0, 100)));
        }
    }

    internal static IReadOnlyList<SoftwareUpdate> Parse(string output)
    {
        var lines = output.Split(['\r', '\n']).Select(l => l.TrimEnd()).Where(l => l.Length > 0).ToList();
        var sepIndex = lines.FindIndex(l => l.Length >= 5 && l.All(c => c == '-'));
        if (sepIndex < 1)
        {
            return [];
        }

        var starts = HeaderColumnRegex().Matches(lines[sepIndex - 1]).Select(m => m.Index).ToList();
        if (starts.Count < 4)
        {
            return [];
        }

        string Cell(string line, int col)
        {
            var from = starts[col];
            if (from >= line.Length)
            {
                return string.Empty;
            }

            var to = col + 1 < starts.Count ? Math.Min(starts[col + 1], line.Length) : line.Length;
            return line[from..to].Trim();
        }

        var result = new List<SoftwareUpdate>();
        foreach (var line in lines.Skip(sepIndex + 1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var name = Cell(line, 0);
            var id = Cell(line, 1);
            var current = Cell(line, 2);
            var available = Cell(line, 3);
            if (id.Length == 0 || id.Contains(' ') || available.Length == 0 || available.Contains(' '))
            {
                continue;
            }

            result.Add(new SoftwareUpdate(name, id, current, available));
        }

        return result;
    }

    private static async Task<(bool Started, int ExitCode, string Output)> RunWingetAsync(string arguments, CancellationToken cancellationToken, Action<string>? onLine = null)
    {
        try
        {
            var startInfo = new ProcessStartInfo("winget.exe", arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return (false, -1, string.Empty);
            }

            // Aucune saisie possible : si winget pose une question (dossier d'installation, confirmation...),
            // il échoue tout de suite au lieu d'attendre indéfiniment une réponse.
            process.StandardInput.Close();

            try
            {
                var output = new StringBuilder();
                var current = new StringBuilder();
                var buffer = new char[1024];
                int read;
                while ((read = await process.StandardOutput.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
                {
                    for (var i = 0; i < read; i++)
                    {
                        var c = buffer[i];
                        output.Append(c);
                        if (c is '\r' or '\n')
                        {
                            if (current.Length > 0)
                            {
                                onLine?.Invoke(current.ToString());
                                current.Clear();
                            }
                        }
                        else
                        {
                            current.Append(c);
                        }
                    }
                }

                if (current.Length > 0)
                {
                    onLine?.Invoke(current.ToString());
                }

                await process.WaitForExitAsync(cancellationToken);
                return (true, process.ExitCode, output.ToString());
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }

                throw;
            }
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return (false, -1, string.Empty);
        }
    }

    [GeneratedRegex(@"\S+(?: \S+)*")]
    private static partial Regex HeaderColumnRegex();

    [GeneratedRegex(@"([\d.,]+)\s*(KB|MB|GB|Ko|Mo|Go)\s*/\s*([\d.,]+)\s*(KB|MB|GB|Ko|Mo|Go)")]
    private static partial Regex SizeProgressRegex();

    [GeneratedRegex(@"(\d{1,3})\s*%")]
    private static partial Regex PercentProgressRegex();

    [GeneratedRegex(@"(?im)^\s*(?:Type du programme d'installation|Type de programme d'installation|Installer Type)\s*:\s*(\w+)")]
    private static partial Regex InstallerTypeRegex();

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9._\-+]*$")]
    private static partial Regex PackageIdRegex();
}
