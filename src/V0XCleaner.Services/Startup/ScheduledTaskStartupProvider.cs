using System.Runtime.InteropServices;
using Microsoft.CSharp.RuntimeBinder;
using V0XCleaner.Core.Models;

namespace V0XCleaner.Services.Startup;

/// <summary>
/// Tâches planifiées dont au moins un déclencheur est "À l'ouverture de session" (TASK_TRIGGER_LOGON = 9).
/// Utilise l'API COM du Planificateur de tâches via liaison dynamique (IDispatch) pour éviter de
/// redéclarer manuellement toute l'interface ITaskService/ITaskFolder/IRegisteredTask.
/// </summary>
internal sealed class ScheduledTaskStartupProvider : IStartupProvider
{
    private const int TaskTriggerLogon = 9;

    public StartupEntrySource Source => StartupEntrySource.ScheduledTask;

    public Task<IReadOnlyList<StartupEntry>> GetEntriesAsync(CancellationToken cancellationToken)
    {
        var entries = new List<StartupEntry>();

        try
        {
            dynamic service = CreateConnectedService();
            dynamic rootFolder = service.GetFolder(@"\");
            CollectFromFolder(rootFolder, entries, cancellationToken);
        }
        catch (Exception ex) when (ex is COMException or InvalidOperationException or UnauthorizedAccessException)
        {
            // Planificateur de tâches indisponible (service arrêté, permissions) : liste vide.
        }

        return Task.FromResult<IReadOnlyList<StartupEntry>>(entries);
    }

    public Task<bool> SetEnabledAsync(StartupEntry entry, bool enabled, CancellationToken cancellationToken)
    {
        try
        {
            dynamic service = CreateConnectedService();
            dynamic rootFolder = service.GetFolder(@"\");
            dynamic task = rootFolder.GetTask(entry.Id);
            task.Enabled = enabled;
            return Task.FromResult(true);
        }
        catch (Exception ex) when (ex is COMException or InvalidOperationException or UnauthorizedAccessException)
        {
            return Task.FromResult(false);
        }
    }

    public Task<bool> DeleteAsync(StartupEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            var idx = entry.Id.LastIndexOf('\\');
            var parentPath = idx <= 0 ? @"\" : entry.Id[..idx];
            var taskName = entry.Id[(idx + 1)..];

            dynamic service = CreateConnectedService();
            dynamic parentFolder = service.GetFolder(parentPath);
            parentFolder.DeleteTask(taskName, 0);
            return Task.FromResult(true);
        }
        catch (Exception ex) when (ex is COMException or InvalidOperationException or UnauthorizedAccessException)
        {
            return Task.FromResult(false);
        }
    }

    private static dynamic CreateConnectedService()
    {
        var type = Type.GetTypeFromProgID("Schedule.Service")
            ?? throw new InvalidOperationException("Le Planificateur de tâches Windows (COM) n'est pas disponible.");

        dynamic service = Activator.CreateInstance(type)!;
        service.Connect();
        return service;
    }

    private static void CollectFromFolder(dynamic folder, List<StartupEntry> entries, CancellationToken cancellationToken)
    {
        dynamic tasks = folder.GetTasks(0);
        foreach (dynamic task in tasks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (!HasLogonTrigger(task))
                {
                    continue;
                }

                entries.Add(new StartupEntry
                {
                    Id = (string)task.Path,
                    Name = (string)task.Name,
                    Command = GetFirstActionPath(task) ?? (string)task.Path,
                    Source = StartupEntrySource.ScheduledTask,
                    IsEnabled = (bool)task.Enabled,
                    Location = (string)task.Path
                });
            }
            catch (Exception ex) when (ex is COMException or RuntimeBinderException)
            {
                // Tâche illisible (corrompue ou permissions insuffisantes) : ignorée.
            }
        }

        dynamic subFolders = folder.GetFolders(0);
        foreach (dynamic sub in subFolders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CollectFromFolder(sub, entries, cancellationToken);
        }
    }

    private static bool HasLogonTrigger(dynamic task)
    {
        dynamic triggers = task.Definition.Triggers;
        foreach (dynamic trigger in triggers)
        {
            if ((int)trigger.Type == TaskTriggerLogon)
            {
                return true;
            }
        }

        return false;
    }

    private static string? GetFirstActionPath(dynamic task)
    {
        try
        {
            dynamic actions = task.Definition.Actions;
            foreach (dynamic action in actions)
            {
                try
                {
                    return (string)action.Path;
                }
                catch (Exception ex) when (ex is COMException or RuntimeBinderException or InvalidCastException)
                {
                    // Type d'action sans propriété Path (ComHandler, etc.) : on essaie l'action suivante.
                }
            }
        }
        catch (Exception ex) when (ex is COMException or RuntimeBinderException)
        {
            // ignoré
        }

        return null;
    }
}
