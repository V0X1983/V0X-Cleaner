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

    public async Task<bool> UpdateAsync(string packageId, CancellationToken cancellationToken = default)
    {
        if (!PackageIdRegex().IsMatch(packageId))
        {
            return false;
        }

        var (started, exitCode, _) = await RunWingetAsync(
            $"upgrade --id {packageId} --exact --silent --accept-package-agreements --accept-source-agreements", cancellationToken);
        logger.LogInformation("Mise à jour winget de {Id} : code {Code}", packageId, exitCode);
        return started && exitCode == 0;
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

    private static async Task<(bool Started, int ExitCode, string Output)> RunWingetAsync(string arguments, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo("winget.exe", arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return (false, -1, string.Empty);
            }

            try
            {
                var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
                await process.WaitForExitAsync(cancellationToken);
                return (true, process.ExitCode, await outputTask);
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

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9._\-+]*$")]
    private static partial Regex PackageIdRegex();
}
