using System.Diagnostics;
using System.Management;
using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.Services;

/// <summary>
/// Services Windows tiers via WMI (Win32_Service). Les services du système et de Microsoft ne sont pas
/// proposés : les désactiver peut empêcher Windows de démarrer correctement.
/// </summary>
public sealed class WindowsServiceManager(ILogger<WindowsServiceManager> logger) : IWindowsServiceManager
{
    public Task<IReadOnlyList<WindowsServiceEntry>> GetServicesAsync(CancellationToken cancellationToken = default) =>
        Task.Run<IReadOnlyList<WindowsServiceEntry>>(() => ReadServices(cancellationToken), cancellationToken);

    public async Task<bool> SetStartModeAsync(string serviceName, ServiceStartMode mode, CancellationToken cancellationToken = default)
    {
        var scMode = mode switch
        {
            ServiceStartMode.Automatic => "auto",
            ServiceStartMode.Manual => "demand",
            _ => "disabled"
        };

        try
        {
            var startInfo = new ProcessStartInfo("sc.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (var argument in new[] { "config", serviceName, "start=", scMode })
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return false;
            }

            await process.WaitForExitAsync(cancellationToken);
            logger.LogInformation("sc config {Service} start= {Mode} : code {Code}", serviceName, scMode, process.ExitCode);
            return process.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            logger.LogWarning(ex, "sc.exe indisponible.");
            return false;
        }
    }

    private List<WindowsServiceEntry> ReadServices(CancellationToken cancellationToken)
    {
        var result = new List<WindowsServiceEntry>();
        var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, DisplayName, PathName, StartMode, State FROM Win32_Service");
            foreach (ManagementBaseObject service in searcher.Get())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var name = service["Name"] as string;
                var pathName = service["PathName"] as string;
                if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(pathName))
                {
                    continue;
                }

                var filePath = CommandLineHelper.ExtractFilePath(pathName);
                if (!IsThirdParty(filePath, windowsDir, out var publisher))
                {
                    continue;
                }

                var startMode = (service["StartMode"] as string) switch
                {
                    "Auto" => ServiceStartMode.Automatic,
                    "Disabled" => ServiceStartMode.Disabled,
                    _ => ServiceStartMode.Manual
                };

                result.Add(new WindowsServiceEntry
                {
                    Name = name,
                    DisplayName = service["DisplayName"] as string ?? name,
                    FilePath = filePath,
                    Publisher = publisher,
                    StartMode = startMode,
                    IsRunning = string.Equals(service["State"] as string, "Running", StringComparison.OrdinalIgnoreCase)
                });
            }
        }
        catch (ManagementException ex)
        {
            logger.LogWarning(ex, "Lecture des services Windows impossible.");
        }

        return result.OrderBy(s => s.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static bool IsThirdParty(string filePath, string windowsDir, out string publisher)
    {
        publisher = string.Empty;
        try
        {
            if (!File.Exists(filePath) || filePath.StartsWith(windowsDir, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            publisher = FileVersionInfo.GetVersionInfo(filePath).CompanyName?.Trim() ?? string.Empty;
            return !publisher.Contains("Microsoft", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }
}
