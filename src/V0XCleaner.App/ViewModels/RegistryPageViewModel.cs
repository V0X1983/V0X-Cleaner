using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.App.ViewModels;

public partial class RegistryPageViewModel : ObservableObject
{
    private readonly IRegistryBackupService _backupService;
    private readonly ILogger<RegistryPageViewModel> _logger;

    public ScanProgress Loading { get; } = new();

    public ObservableCollection<CleaningSectionViewModel> Groups { get; } = [];

    public ObservableCollection<string> AvailableBackups { get; } = [];

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Cliquez sur \"Analyser\" pour rechercher les problèmes de registre.";

    [ObservableProperty]
    private bool _hasScanned;

    [ObservableProperty]
    private bool _simulationMode;

    [ObservableProperty]
    private bool _canRepairNow;

    [ObservableProperty]
    private int _totalIssuesFound;

    [ObservableProperty]
    private string? _selectedBackup;

    public RegistryPageViewModel(IRegistryIssueCatalog catalog, IRegistryBackupService backupService, ILogger<RegistryPageViewModel> logger)
    {
        _backupService = backupService;
        _logger = logger;

        var taskViewModels = catalog.GetTasks()
            .Select(t => new CleaningTaskViewModel(t))
            .ToList();

        foreach (var taskVm in taskViewModels)
        {
            taskVm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(CleaningTaskViewModel.IsSelected) or nameof(CleaningTaskViewModel.FoundItemsCount))
                {
                    RecomputeTotal();
                }
            };
        }

        foreach (var group in taskViewModels.GroupBy(t => t.Task.SubGroupLabel ?? t.Task.Section.ToString()))
        {
            Groups.Add(new CleaningSectionViewModel(group.Key, group.ToList()));
        }

        RefreshBackups();
    }

    private IEnumerable<CleaningTaskViewModel> AllTasks => Groups.SelectMany(g => g.Tasks);

    [RelayCommand(CanExecute = nameof(CanRunCommands))]
    private async Task AnalyzeAsync()
    {
        IsBusy = true;
        HasScanned = false;
        StatusMessage = "Analyse du registre en cours...";
        var ct = Loading.Begin("Analyse du registre en cours...");
        var selectedCount = Math.Max(1, AllTasks.Count(t => t.IsSelected));
        var done = 0;

        try
        {
            foreach (var taskVm in AllTasks)
            {
                if (!taskVm.IsSelected)
                {
                    taskVm.ResetScanState();
                    continue;
                }

                taskVm.Status = CleaningTaskStatus.Scanning;
                Loading.Message = $"Analyse : {taskVm.Task.DisplayName}";
                try
                {
                    var result = await Task.Run(() => taskVm.Task.Scanner.ScanAsync(ct), ct);
                    taskVm.LastScanItems = result.Items;
                    taskVm.FoundItemsCount = result.Items.Count;
                    taskVm.FoundSizeBytes = 0;
                    taskVm.Status = CleaningTaskStatus.Scanned;
                    taskVm.LastError = null;
                    done++;
                    Loading.Progress = done * 100.0 / selectedCount;
                }
                catch (OperationCanceledException)
                {
                    taskVm.ResetScanState();
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur pendant l'analyse registre de {Key}", taskVm.Task.Key);
                    taskVm.Status = CleaningTaskStatus.Error;
                    taskVm.LastError = ex.Message;
                }
            }

            RecomputeTotal();
            HasScanned = true;
            StatusMessage = TotalIssuesFound > 0
                ? $"Analyse terminée : {TotalIssuesFound} problème(s) trouvé(s)."
                : "Analyse terminée : aucun problème trouvé.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Analyse arrêtée.";
        }
        finally
        {
            Loading.End();
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRepair))]
    private async Task RepairAsync()
    {
        IsBusy = true;
        StatusMessage = SimulationMode ? "Simulation en cours..." : "Sauvegarde puis réparation en cours...";

        try
        {
            var repaired = 0;
            var failedCount = 0;
            var mode = SimulationMode ? OperationMode.Simulate : OperationMode.Execute;

            foreach (var taskVm in AllTasks)
            {
                if (!taskVm.IsSelected || taskVm.LastScanItems.Count == 0)
                {
                    continue;
                }

                taskVm.Status = CleaningTaskStatus.Cleaning;
                try
                {
                    var result = await Task.Run(() => taskVm.Task.Cleaner.CleanAsync(taskVm.LastScanItems, mode));
                    repaired += result.SucceededCount;
                    failedCount += result.FailedCount;

                    var finalStatus = result.FailedCount == 0 ? CleaningTaskStatus.Cleaned : CleaningTaskStatus.Error;
                    var finalError = result.Errors.Count > 0
                        ? $"{result.Errors.Count} élément(s) n'ont pas pu être réparés (droits administrateur parfois requis)."
                        : null;

                    if (mode == OperationMode.Execute)
                    {
                        taskVm.ResetScanState();
                    }

                    taskVm.Status = finalStatus;
                    taskVm.LastError = finalError;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur pendant la réparation registre de {Key}", taskVm.Task.Key);
                    taskVm.Status = CleaningTaskStatus.Error;
                    taskVm.LastError = ex.Message;
                }
            }

            RecomputeTotal();
            var verb = SimulationMode ? "Simulation terminée" : "Réparation terminée";
            StatusMessage = failedCount == 0
                ? $"{verb} : {repaired} élément(s) traité(s)."
                : $"{verb} avec {failedCount} échec(s) : {repaired} élément(s) traité(s).";

            if (mode == OperationMode.Execute)
            {
                HasScanned = false;
                RefreshBackups();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRestore))]
    private async Task RestoreBackupAsync()
    {
        if (string.IsNullOrEmpty(SelectedBackup))
        {
            return;
        }

        IsBusy = true;
        StatusMessage = "Restauration de la sauvegarde en cours...";

        try
        {
            var ok = await Task.Run(() => _backupService.RestoreBackupAsync(SelectedBackup!));
            StatusMessage = ok
                ? "Sauvegarde restaurée avec succès."
                : "Échec de la restauration de la sauvegarde.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void RefreshBackups()
    {
        var selected = SelectedBackup;
        AvailableBackups.Clear();
        foreach (var backup in _backupService.ListBackups())
        {
            AvailableBackups.Add(backup);
        }

        SelectedBackup = selected is not null && AvailableBackups.Contains(selected)
            ? selected
            : AvailableBackups.FirstOrDefault();
    }

    private bool CanRunCommands() => !IsBusy;

    private bool CanRepair() => !IsBusy && HasScanned && TotalIssuesFound > 0;

    private bool CanRestore() => !IsBusy && !string.IsNullOrEmpty(SelectedBackup);

    partial void OnIsBusyChanged(bool value)
    {
        AnalyzeCommand.NotifyCanExecuteChanged();
        RepairCommand.NotifyCanExecuteChanged();
        RestoreBackupCommand.NotifyCanExecuteChanged();
        CanRepairNow = CanRepair();
    }

    partial void OnHasScannedChanged(bool value)
    {
        RepairCommand.NotifyCanExecuteChanged();
        CanRepairNow = CanRepair();
    }

    partial void OnSelectedBackupChanged(string? value) => RestoreBackupCommand.NotifyCanExecuteChanged();

    private void RecomputeTotal()
    {
        TotalIssuesFound = AllTasks.Where(t => t.IsSelected).Sum(t => t.FoundItemsCount);
        RepairCommand.NotifyCanExecuteChanged();
        CanRepairNow = CanRepair();
    }
}
