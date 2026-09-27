using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models.Elevation;
using V0XCleaner.Services.Native;

namespace V0XCleaner.Services.Elevation;

/// <summary>
/// Implémentation réelle de <see cref="IElevatedOperationClient"/> : déploie puis relance
/// V0XCleaner.ElevatedHelper élevé (invite UAC) une seule fois pour tout le lot d'opérations,
/// en lui passant la requête via un fichier JSON temporaire et en récupérant sa réponse de la
/// même façon (pas de pipe nommé : le helper est un process ponctuel, pas un serveur persistant —
/// voir PROMPT.md, section Phase 2).
/// </summary>
public sealed class ProcessElevatedOperationClient(ILogger<ProcessElevatedOperationClient> logger) : IElevatedOperationClient
{
    public bool IsSupported => true;

    public async Task<ElevatedBatchResponse> ExecuteAsync(
        IReadOnlyList<ElevatedRegistryOperation> operations,
        CancellationToken cancellationToken = default)
    {
        var launcher = new ElevatedHelperLauncher();
        if (!launcher.TryEnsureDeployed())
        {
            return new ElevatedBatchResponse(false, "Le helper d'élévation V0XCleaner.ElevatedHelper est introuvable.", []);
        }

        var inputPath = Path.Combine(AppPaths.ElevatedTempFolder, $"in-{Guid.NewGuid():N}.json");
        var outputPath = Path.Combine(AppPaths.ElevatedTempFolder, $"out-{Guid.NewGuid():N}.json");

        try
        {
            var request = new ElevatedBatchRequest(operations);
            await File.WriteAllTextAsync(inputPath, JsonSerializer.Serialize(request), cancellationToken);

            var startInfo = new ProcessStartInfo(launcher.DeployedHelperExePath!)
            {
                UseShellExecute = true,
                Verb = "runas",
            };
            startInfo.ArgumentList.Add(inputPath);
            startInfo.ArgumentList.Add(outputPath);

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return new ElevatedBatchResponse(false, "Impossible de démarrer le helper élevé.", []);
            }

            await process.WaitForExitAsync(cancellationToken);

            if (!File.Exists(outputPath))
            {
                return new ElevatedBatchResponse(
                    false,
                    $"Le helper élevé ne s'est pas terminé correctement (code de sortie {process.ExitCode}).",
                    []);
            }

            var json = await File.ReadAllTextAsync(outputPath, cancellationToken);
            var response = JsonSerializer.Deserialize<ElevatedBatchResponse>(json);
            return response ?? new ElevatedBatchResponse(false, "Réponse du helper élevé illisible.", []);
        }
        catch (Win32Exception ex)
        {
            // L'utilisateur a refusé l'invite UAC, ou l'élévation a échoué.
            logger.LogInformation(ex, "Élévation du helper refusée ou impossible.");
            return new ElevatedBatchResponse(false, "Élévation refusée par l'utilisateur.", []);
        }
        finally
        {
            TryDelete(inputPath);
            TryDelete(outputPath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Le helper élevé peut détenir le fichier un court instant après sa propre écriture ; sans conséquence.
        }
    }
}
