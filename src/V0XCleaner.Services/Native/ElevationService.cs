using System.ComponentModel;
using System.Diagnostics;
using System.Security;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;

namespace V0XCleaner.Services.Native;

/// <summary>Détection d'élévation UAC et relance élevée à la demande (voir IElevationService).</summary>
public sealed class ElevationService(ILogger<ElevationService> logger) : IElevationService
{
    public bool IsElevated { get; } = ComputeIsElevated();

    public bool RelaunchElevated()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo(exePath)
            {
                UseShellExecute = true,
                Verb = "runas"
            };

            using var process = Process.Start(startInfo);
            return process is not null;
        }
        catch (Win32Exception ex)
        {
            // L'utilisateur a refusé l'invite UAC, ou l'élévation a échoué.
            logger.LogInformation(ex, "Relance élevée annulée ou impossible.");
            return false;
        }
    }

    private static bool ComputeIsElevated()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch (SecurityException)
        {
            return false;
        }
    }
}
