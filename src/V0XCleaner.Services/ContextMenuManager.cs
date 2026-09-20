using System.Diagnostics;
using System.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.Services;

/// <summary>
/// Entrées du menu contextuel de l'Explorateur ajoutées par des logiciels tiers. Un verbe (« shell »)
/// se désactive avec la valeur LegacyDisable ; une extension COM (« ContextMenuHandlers ») se bloque
/// via la liste « Shell Extensions\Blocked » de HKLM (droits administrateur). Les entrées de Windows
/// et de Microsoft ne sont pas proposées.
/// </summary>
public sealed class ContextMenuManager(ILogger<ContextMenuManager> logger) : IContextMenuManager
{
    private const string BlockedKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked";
    private const string LegacyDisableValue = "LegacyDisable";

    private static readonly (ContextMenuScope Scope, string Path)[] Locations =
    [
        (ContextMenuScope.Directory, "Directory"),
        (ContextMenuScope.Background, @"Directory\Background"),
        (ContextMenuScope.File, "*")
    ];

    public Task<IReadOnlyList<ContextMenuEntry>> GetEntriesAsync(CancellationToken cancellationToken = default) =>
        Task.Run<IReadOnlyList<ContextMenuEntry>>(() => ReadEntries(cancellationToken), cancellationToken);

    public Task<bool> SetEnabledAsync(ContextMenuEntry entry, bool enabled, CancellationToken cancellationToken = default) =>
        Task.Run(() => entry.Kind == ContextMenuEntryKind.Verb ? SetVerbEnabled(entry, enabled) : SetHandlerEnabled(entry, enabled), cancellationToken);

    private List<ContextMenuEntry> ReadEntries(CancellationToken cancellationToken)
    {
        var result = new List<ContextMenuEntry>();
        var blocked = ReadBlockedClsids();
        var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        using var classes = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64);
        foreach (var (scope, path) in Locations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var verbs = classes.OpenSubKey(path + @"\shell"))
            {
                foreach (var name in verbs?.GetSubKeyNames() ?? [])
                {
                    using var verb = verbs!.OpenSubKey(name);
                    using var command = verb?.OpenSubKey("command");
                    if (verb is null || command?.GetValue(string.Empty) is not string commandLine || string.IsNullOrWhiteSpace(commandLine))
                    {
                        continue;
                    }

                    var filePath = CommandLineHelper.ExtractFilePath(commandLine);
                    if (!IsThirdParty(filePath, windowsDir, out var publisher))
                    {
                        continue;
                    }

                    var label = (verb.GetValue("MUIVerb") ?? verb.GetValue(string.Empty)) as string;
                    result.Add(new ContextMenuEntry
                    {
                        Name = string.IsNullOrWhiteSpace(label) || label.StartsWith('@') ? name : label.Replace("&", string.Empty),
                        FilePath = filePath,
                        Publisher = publisher,
                        Scope = scope,
                        Kind = ContextMenuEntryKind.Verb,
                        RegistryPath = path + @"\shell\" + name,
                        IsEnabled = verb.GetValue(LegacyDisableValue) is null
                    });
                }
            }

            using (var handlers = classes.OpenSubKey(path + @"\shellex\ContextMenuHandlers"))
            {
                foreach (var name in handlers?.GetSubKeyNames() ?? [])
                {
                    using var handler = handlers!.OpenSubKey(name);
                    var clsid = handler?.GetValue(string.Empty) as string;
                    if (clsid is null || !IsClsid(clsid))
                    {
                        // Le nom de la clé peut lui-même être le CLSID ; sinon il n'y a rien à résoudre.
                        clsid = IsClsid(name) ? name : null;
                    }

                    if (clsid is null)
                    {
                        continue;
                    }

                    var server = ResolveServerPath(classes, clsid, out var clsidName);
                    if (server is null || !IsThirdParty(server, windowsDir, out var publisher))
                    {
                        continue;
                    }

                    result.Add(new ContextMenuEntry
                    {
                        Name = IsClsid(name) && !string.IsNullOrWhiteSpace(clsidName) ? clsidName : name,
                        FilePath = server,
                        Publisher = publisher,
                        Scope = scope,
                        Kind = ContextMenuEntryKind.Handler,
                        RegistryPath = path + @"\shellex\ContextMenuHandlers\" + name,
                        Clsid = clsid,
                        IsEnabled = !blocked.Contains(clsid)
                    });
                }
            }
        }

        return result;
    }

    private bool SetVerbEnabled(ContextMenuEntry entry, bool enabled)
    {
        var found = false;
        foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = baseKey.OpenSubKey(@"Software\Classes\" + entry.RegistryPath, writable: true);
                if (key is null)
                {
                    continue;
                }

                found = true;
                if (enabled)
                {
                    key.DeleteValue(LegacyDisableValue, throwOnMissingValue: false);
                }
                else
                {
                    key.SetValue(LegacyDisableValue, string.Empty, RegistryValueKind.String);
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException)
            {
                logger.LogWarning(ex, "Modification impossible de {Path} ({Hive})", entry.RegistryPath, hive);
                return false;
            }
        }

        return found;
    }

    private bool SetHandlerEnabled(ContextMenuEntry entry, bool enabled)
    {
        if (entry.Clsid is null)
        {
            return false;
        }

        try
        {
            using var localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var blocked = localMachine.CreateSubKey(BlockedKeyPath, writable: true);
            if (enabled)
            {
                blocked.DeleteValue(entry.Clsid, throwOnMissingValue: false);
            }
            else
            {
                blocked.SetValue(entry.Clsid, "Bloqué par V0X Cleaner", RegistryValueKind.String);
            }

            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException)
        {
            logger.LogWarning(ex, "Blocage impossible de l'extension {Clsid}", entry.Clsid);
            return false;
        }
    }

    private static HashSet<string> ReadBlockedClsids()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var blocked = baseKey.OpenSubKey(BlockedKeyPath);
                foreach (var name in blocked?.GetValueNames() ?? [])
                {
                    result.Add(name);
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException)
            {
                // Liste illisible : les extensions sont alors considérées comme actives.
            }
        }

        return result;
    }

    private static string? ResolveServerPath(RegistryKey classes, string clsid, out string? displayName)
    {
        using var key = classes.OpenSubKey(@"CLSID\" + clsid);
        displayName = key?.GetValue(string.Empty) as string;

        foreach (var serverKey in new[] { "InprocServer32", "LocalServer32" })
        {
            using var server = key?.OpenSubKey(serverKey);
            if (server?.GetValue(string.Empty) is string value && !string.IsNullOrWhiteSpace(value))
            {
                return CommandLineHelper.ExtractFilePath(value);
            }
        }

        return null;
    }

    private static bool IsClsid(string value) => value.Length == 38 && value[0] == '{' && value[^1] == '}' && Guid.TryParse(value, out _);

    /// <summary>Vrai si le fichier existe hors du dossier Windows et n'est pas signé Microsoft.</summary>
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
