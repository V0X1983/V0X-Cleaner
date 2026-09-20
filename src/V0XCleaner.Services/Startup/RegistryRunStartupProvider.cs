using Microsoft.Win32;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.RegistryCleanup;

namespace V0XCleaner.Services.Startup;

/// <summary>
/// Programmes lancés via les clés Run du registre (HKCU, HKLM, et la vue 32 bits sous WOW6432Node).
/// RunOnce est volontairement exclu : ces valeurs s'auto-suppriment après une exécution et ne
/// représentent pas un programme de démarrage persistant.
/// </summary>
internal sealed class RegistryRunStartupProvider : IStartupProvider
{
    private const string ApprovedSubPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    private static readonly (RegistryKey Hive, string HivePrefix, string RunSubPath)[] Roots =
    [
        (Registry.CurrentUser, "HKCU", @"Software\Microsoft\Windows\CurrentVersion\Run"),
        (Registry.LocalMachine, "HKLM", @"Software\Microsoft\Windows\CurrentVersion\Run"),
        (Registry.LocalMachine, "HKLM", @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run")
    ];

    public StartupEntrySource Source => StartupEntrySource.RegistryRun;

    public Task<IReadOnlyList<StartupEntry>> GetEntriesAsync(CancellationToken cancellationToken)
    {
        var entries = new List<StartupEntry>();

        foreach (var (hive, hivePrefix, runSubPath) in Roots)
        {
            using var runKey = RegistrySafe.OpenSubKey(hive, runSubPath);
            if (runKey is null)
            {
                continue;
            }

            foreach (var valueName in RegistrySafe.GetValueNames(runKey))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(valueName))
                {
                    continue;
                }

                if (RegistrySafe.GetValue(runKey, valueName) is not string command || string.IsNullOrWhiteSpace(command))
                {
                    continue;
                }

                var enabled = StartupApprovedHelper.IsEnabled(hive, ApprovedSubPath, valueName);

                entries.Add(new StartupEntry
                {
                    Id = $"{hivePrefix}||{runSubPath}||{valueName}",
                    Name = valueName,
                    Command = command,
                    Source = Source,
                    IsEnabled = enabled,
                    Location = $@"{(hivePrefix == "HKLM" ? "HKEY_LOCAL_MACHINE" : "HKEY_CURRENT_USER")}\{runSubPath}",
                    RequiresElevation = hivePrefix == "HKLM"
                });
            }
        }

        return Task.FromResult<IReadOnlyList<StartupEntry>>(entries);
    }

    public Task<bool> SetEnabledAsync(StartupEntry entry, bool enabled, CancellationToken cancellationToken)
    {
        var (hive, _, valueName) = Parse(entry.Id);
        try
        {
            StartupApprovedHelper.SetEnabled(hive, ApprovedSubPath, valueName, enabled);
            return Task.FromResult(true);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
        {
            return Task.FromResult(false);
        }
    }

    public Task<bool> DeleteAsync(StartupEntry entry, CancellationToken cancellationToken)
    {
        var (hive, runSubPath, valueName) = Parse(entry.Id);
        try
        {
            using var runKey = hive.OpenSubKey(runSubPath, writable: true);
            runKey?.DeleteValue(valueName, throwOnMissingValue: false);
            return Task.FromResult(true);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
        {
            return Task.FromResult(false);
        }
    }

    private static (RegistryKey Hive, string RunSubPath, string ValueName) Parse(string id)
    {
        var parts = id.Split("||");
        var hive = parts[0] == "HKLM" ? Registry.LocalMachine : Registry.CurrentUser;
        return (hive, parts[1], parts[2]);
    }
}
