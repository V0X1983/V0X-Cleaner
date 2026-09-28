using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using V0XCleaner.App.Helpers;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.App.ViewModels;

public partial class CleanerPageViewModel : ObservableObject
{
    private readonly ILogger<CleanerPageViewModel> _logger;
    private readonly ISettingsService _settings;

    public ScanProgress Loading { get; } = new();

    public ObservableCollection<CleaningSectionViewModel> Sections { get; } = [];

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Cliquez sur \"Analyser\" pour rechercher les éléments à nettoyer.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedTotalRecoverable))]
    private long _totalRecoverableBytes;

    [ObservableProperty]
    private bool _hasScanned;

    [ObservableProperty]
    private bool _simulationMode;

    [ObservableProperty]
    private bool _canCleanNow;

    public string FormattedTotalRecoverable => ByteFormatter.Format(TotalRecoverableBytes);

    public CleanerPageViewModel(ICleaningCatalog catalog, ISettingsService settings, ILogger<CleanerPageViewModel> logger)
    {
        _settings = settings;
        _logger = logger;

        var taskViewModels = catalog.GetTasks()
            .Select(t => new CleaningTaskViewModel(t))
            .ToList();

        foreach (var taskVm in taskViewModels)
        {
            taskVm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(CleaningTaskViewModel.IsSelected) or nameof(CleaningTaskViewModel.FoundSizeBytes))
                {
                    RecomputeTotal();
                }
            };
        }

        foreach (var group in taskViewModels.GroupBy(t => t.Task.Section))
        {
            Sections.Add(new CleaningSectionViewModel(SectionTitle(group.Key), group.ToList()));
        }
    }

    private static string SectionTitle(CleaningSection section) => section switch
    {
        CleaningSection.System => "Système",
        CleaningSection.Browsers => "Navigateurs",
        CleaningSection.ThirdPartyApplications => "Applications tierces",
        CleaningSection.Cloud => "Stockage cloud",
        _ => section.ToString()
    };

    private IEnumerable<CleaningTaskViewModel> AllTasks => Sections.SelectMany(s => s.Tasks);

    [RelayCommand(CanExecute = nameof(CanRunCommands))]
    private async Task AnalyzeAsync()
    {
        IsBusy = true;
        HasScanned = false;
        StatusMessage = "Analyse en cours...";
        var ct = Loading.Begin("Analyse en cours...");
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
                    taskVm.FoundSizeBytes = result.TotalSizeBytes;
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
                    _logger.LogError(ex, "Erreur pendant l'analyse de {Key}", taskVm.Task.Key);
                    taskVm.Status = CleaningTaskStatus.Error;
                    taskVm.LastError = ex.Message;
                }
            }

            RecomputeTotal();
            HasScanned = true;
            StatusMessage = $"Analyse terminée : {FormattedTotalRecoverable} récupérables.";
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

    [RelayCommand(CanExecute = nameof(CanClean))]
    private async Task CleanAsync()
    {
        IsBusy = true;
        StatusMessage = SimulationMode ? "Simulation en cours..." : "Nettoyage en cours...";
        var ct = Loading.Begin(StatusMessage, "Arrêter le nettoyage");
        var toClean = Math.Max(1, AllTasks.Count(t => t.IsSelected && t.LastScanItems.Count > 0));
        var cleaned = 0;

        try
        {
            long freed = 0;
            var failedCount = 0;
            var mode = SimulationMode ? OperationMode.Simulate : OperationMode.Execute;

            foreach (var taskVm in AllTasks)
            {
                if (!taskVm.IsSelected || taskVm.LastScanItems.Count == 0)
                {
                    continue;
                }

                taskVm.Status = CleaningTaskStatus.Cleaning;
                var verbLabel = SimulationMode ? "Simulation" : "Nettoyage";
                Loading.Message = $"{verbLabel} en cours : {taskVm.Task.DisplayName} ({taskVm.LastScanItems.Count:N0} élément(s))...";
                try
                {
                    var result = await Task.Run(() => taskVm.Task.Cleaner.CleanAsync(taskVm.LastScanItems, mode, ct), ct);
                    cleaned++;
                    Loading.Progress = cleaned * 100.0 / toClean;
                    freed += result.FreedBytes;
                    failedCount += result.FailedCount;

                    var finalStatus = result.FailedCount == 0 ? CleaningTaskStatus.Cleaned : CleaningTaskStatus.Error;
                    var finalError = result.Errors.Count > 0
                        ? $"{result.Errors.Count} élément(s) n'ont pas pu être traités."
                        : null;

                    if (mode == OperationMode.Execute)
                    {
                        taskVm.ResetScanState();
                    }

                    taskVm.Status = finalStatus;
                    taskVm.LastError = finalError;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur pendant le nettoyage de {Key}", taskVm.Task.Key);
                    taskVm.Status = CleaningTaskStatus.Error;
                    taskVm.LastError = ex.Message;
                }
            }

            RecomputeTotal();
            var verb = SimulationMode ? "Simulation terminée" : "Nettoyage terminé";
            var freedLabel = SimulationMode ? "seraient libérés" : "libérés";
            StatusMessage = failedCount == 0
                ? $"{verb} : {ByteFormatter.Format(freed)} {freedLabel}."
                : $"{verb} avec {failedCount} erreur(s) : {ByteFormatter.Format(freed)} {freedLabel}.";

            if (mode == OperationMode.Execute)
            {
                HasScanned = false;
                _settings.Current.LastCleanUtc = DateTime.UtcNow;
                _settings.Current.LastCleanFreedBytes = freed;
                await _settings.SaveAsync();
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Nettoyage arrêté.";
        }
        finally
        {
            Loading.End();
            IsBusy = false;
        }
    }

    private bool CanRunCommands() => !IsBusy;

    private bool CanClean() => !IsBusy && HasScanned && TotalRecoverableBytes > 0;

    partial void OnIsBusyChanged(bool value)
    {
        AnalyzeCommand.NotifyCanExecuteChanged();
        CleanCommand.NotifyCanExecuteChanged();
        CanCleanNow = CanClean();
    }

    partial void OnHasScannedChanged(bool value)
    {
        CleanCommand.NotifyCanExecuteChanged();
        CanCleanNow = CanClean();
    }

    private void RecomputeTotal()
    {
        TotalRecoverableBytes = AllTasks.Where(t => t.IsSelected).Sum(t => t.FoundSizeBytes);
        CleanCommand.NotifyCanExecuteChanged();
        CanCleanNow = CanClean();
    }
}
