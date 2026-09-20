using Microsoft.Win32;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.FileSystem;
using V0XCleaner.Services.Native;

namespace V0XCleaner.Services.Startup;

/// <summary>Raccourcis présents dans le dossier Démarrage de l'utilisateur et celui commun à tous les utilisateurs.</summary>
internal sealed class StartupFolderStartupProvider : IStartupProvider
{
    private const string ApprovedSubPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder";

    private static readonly (RegistryKey Hive, string HivePrefix, string FolderPath)[] Roots =
    [
        (Registry.CurrentUser, "HKCU", Environment.GetFolderPath(Environment.SpecialFolder.Startup)),
        (Registry.LocalMachine, "HKLM", Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup))
    ];

    public StartupEntrySource Source => StartupEntrySource.StartupFolder;

    public Task<IReadOnlyList<StartupEntry>> GetEntriesAsync(CancellationToken cancellationToken)
    {
        var entries = new List<StartupEntry>();

        foreach (var (hive, hivePrefix, folderPath) in Roots)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(folderPath, "*.lnk"))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileName = Path.GetFileName(file);
                var target = ShellLinkResolver.ResolveTarget(file);
                var enabled = StartupApprovedHelper.IsEnabled(hive, ApprovedSubPath, fileName);

                entries.Add(new StartupEntry
                {
                    Id = $"{hivePrefix}||{file}",
                    Name = Path.GetFileNameWithoutExtension(file),
                    Command = string.IsNullOrWhiteSpace(target) ? file : target,
                    Source = Source,
                    IsEnabled = enabled,
                    Location = folderPath
                });
            }
        }

        return Task.FromResult<IReadOnlyList<StartupEntry>>(entries);
    }

    public Task<bool> SetEnabledAsync(StartupEntry entry, bool enabled, CancellationToken cancellationToken)
    {
        var (hive, filePath) = Parse(entry.Id);
        try
        {
            StartupApprovedHelper.SetEnabled(hive, ApprovedSubPath, Path.GetFileName(filePath), enabled);
            return Task.FromResult(true);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
        {
            return Task.FromResult(false);
        }
    }

    public Task<bool> DeleteAsync(StartupEntry entry, CancellationToken cancellationToken)
    {
        var (_, filePath) = Parse(entry.Id);
        var ok = FileSystemHelpers.TryDeleteFile(filePath, out _);
        return Task.FromResult(ok);
    }

    private static (RegistryKey Hive, string FilePath) Parse(string id)
    {
        var idx = id.IndexOf("||", StringComparison.Ordinal);
        var hivePrefix = id[..idx];
        var filePath = id[(idx + 2)..];
        var hive = hivePrefix == "HKLM" ? Registry.LocalMachine : Registry.CurrentUser;
        return (hive, filePath);
    }
}
