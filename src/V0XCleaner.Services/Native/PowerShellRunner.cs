using System.Diagnostics;
using System.Text;

namespace V0XCleaner.Services.Native;

/// <summary>
/// Exécute un script PowerShell via -EncodedCommand (Base64/UTF-16LE) : élimine tout problème
/// d'échappement de guillemets qui surviendrait en passant le script tel quel sur la ligne de
/// commande. Utilisé pour piloter les paquets AppX (Get-AppxPackage / Remove-AppxPackage),
/// qui n'ont pas d'équivalent simple via l'API Win32 classique depuis une app non empaquetée.
/// </summary>
internal static class PowerShellRunner
{
    public static async Task<(bool Success, string StandardOutput, string StandardError, int ExitCode)> RunAsync(
        string script, CancellationToken cancellationToken = default)
    {
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

        var startInfo = new ProcessStartInfo("powershell.exe",
            $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {encoded}")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return (false, string.Empty, "Impossible de démarrer PowerShell.", -1);
            }

            var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var stdOut = await stdOutTask;
            var stdErr = await stdErrTask;
            return (process.ExitCode == 0, stdOut, stdErr, process.ExitCode);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return (false, string.Empty, ex.Message, -1);
        }
    }
}
