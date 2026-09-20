using System.Runtime.InteropServices;
using Microsoft.CSharp.RuntimeBinder;
using V0XCleaner.Core.Abstractions;

namespace V0XCleaner.Services;

/// <summary>
/// Crée/retire une tâche planifiée Windows qui relance V0X Cleaner avec "--silent --clean" à
/// l'heure et la fréquence choisies. Utilise la même liaison dynamique COM que le gestionnaire
/// de démarrage (Étape 3) pour piloter le Planificateur de tâches.
/// </summary>
public sealed class AutoCleanScheduler : IAutoCleanScheduler
{
    private const string TaskName = "V0XCleaner AutoClean";
    private const int TaskTriggerDaily = 2;
    private const int TaskTriggerWeekly = 3;
    private const int TaskActionExec = 0;
    private const int TaskLogonInteractiveToken = 3;
    private const int TaskCreateOrUpdate = 6;

    public Task<bool> ConfigureAsync(bool enabled, string frequency, int hour, CancellationToken cancellationToken = default)
    {
        try
        {
            dynamic service = CreateConnectedService();
            dynamic rootFolder = service.GetFolder(@"\");

            try
            {
                rootFolder.DeleteTask(TaskName, 0);
            }
            catch (Exception ex) when (ex is COMException or FileNotFoundException or RuntimeBinderException)
            {
                // Aucune tâche existante à supprimer : normal au premier réglage. Le HRESULT
                // "fichier introuvable" remonte ici sous forme de FileNotFoundException (et non
                // COMException) à cause du chemin de marshaling emprunté par l'appel dynamique.
            }

            if (!enabled)
            {
                return Task.FromResult(true);
            }

            dynamic taskDefinition = service.NewTask(0);
            taskDefinition.RegistrationInfo.Description = "Nettoyage automatique planifié par V0X Cleaner.";

            dynamic triggers = taskDefinition.Triggers;
            var isWeekly = string.Equals(frequency, "Weekly", StringComparison.OrdinalIgnoreCase);
            dynamic trigger = triggers.Create(isWeekly ? TaskTriggerWeekly : TaskTriggerDaily);
            trigger.StartBoundary = DateTime.Today.AddHours(Math.Clamp(hour, 0, 23)).ToString("yyyy-MM-ddTHH:mm:ss");
            if (isWeekly)
            {
                trigger.DaysOfWeek = 1; // Lundi
            }

            dynamic action = taskDefinition.Actions.Create(TaskActionExec);
            action.Path = Environment.ProcessPath;
            action.Arguments = "--silent --clean";

            taskDefinition.Principal.LogonType = TaskLogonInteractiveToken;
            taskDefinition.Settings.Enabled = true;
            taskDefinition.Settings.StartWhenAvailable = true;

            rootFolder.RegisterTaskDefinition(TaskName, taskDefinition, TaskCreateOrUpdate, null, null, TaskLogonInteractiveToken);
            return Task.FromResult(true);
        }
        catch (Exception ex) when (ex is COMException or FileNotFoundException or RuntimeBinderException or InvalidOperationException)
        {
            return Task.FromResult(false);
        }
    }

    public Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            dynamic service = CreateConnectedService();
            dynamic rootFolder = service.GetFolder(@"\");
            dynamic task = rootFolder.GetTask(TaskName);
            return Task.FromResult(task is not null);
        }
        catch (Exception ex) when (ex is COMException or FileNotFoundException or RuntimeBinderException or InvalidOperationException)
        {
            return Task.FromResult(false);
        }
    }

    private static dynamic CreateConnectedService()
    {
        var type = Type.GetTypeFromProgID("Schedule.Service")
            ?? throw new InvalidOperationException("Le Planificateur de tâches Windows n'est pas disponible.");

        dynamic service = Activator.CreateInstance(type)!;
        service.Connect();
        return service;
    }
}
