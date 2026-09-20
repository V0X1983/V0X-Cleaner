using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using V0XCleaner.App.Helpers;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.App.ViewModels;

public partial class HealthCheckViewModel : ObservableObject
{
    private readonly IHealthCheckService _healthCheck;
    private readonly ICleaningCatalog _cleaningCatalog;
    private readonly IMemoryOptimizer _memoryOptimizer;
    private readonly ILogger<HealthCheckViewModel> _logger;

    public ObservableCollection<HealthCheckItemViewModel> Items { get; } = [];

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private int _overallScore;

    [ObservableProperty]
    private string _statusMessage = "Cliquez sur \"Analyser\" pour calculer le bilan de santé.";

    public HealthCheckViewModel(
        IHealthCheckService healthCheck,
        ICleaningCatalog cleaningCatalog,
        IMemoryOptimizer memoryOptimizer,
        ILogger<HealthCheckViewModel> logger)
    {
        _healthCheck = healthCheck;
        _cleaningCatalog = cleaningCatalog;
        _memoryOptimizer = memoryOptimizer;
        _logger = logger;
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        IsBusy = true;
        StatusMessage = "Analyse en cours...";

        try
        {
            var result = await Task.Run(() => _healthCheck.RunAsync());
            OverallScore = result.OverallScore;
            Items.Clear();
            foreach (var item in result.Items)
            {
                Items.Add(new HealthCheckItemViewModel(item));
            }

            StatusMessage = $"Bilan terminé — score global : {OverallScore}/100.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur pendant le bilan de santé.");
            StatusMessage = "Erreur pendant le bilan de santé.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task QuickCleanAsync()
    {
        IsBusy = true;
        StatusMessage = "Nettoyage rapide en cours...";

        try
        {
            long freed = 0;
            foreach (var task in _cleaningCatalog.GetTasks().Where(t => t.SelectedByDefault && t.Section == CleaningSection.System))
            {
                var scan = await Task.Run(() => task.Scanner.ScanAsync());
                if (scan.Items.Count > 0)
                {
                    var result = await Task.Run(() => task.Cleaner.CleanAsync(scan.Items, OperationMode.Execute));
                    freed += result.FreedBytes;
                }
            }

            StatusMessage = $"Nettoyage rapide terminé : {ByteFormatter.Format(freed)} libérés.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur pendant le nettoyage rapide.");
            StatusMessage = "Erreur pendant le nettoyage rapide.";
        }
        finally
        {
            IsBusy = false;
        }

        await RunAsync();
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task FreeMemoryAsync()
    {
        IsBusy = true;
        StatusMessage = "Libération de la mémoire en cours...";

        try
        {
            var result = await Task.Run(() => _memoryOptimizer.FreeMemoryAsync());
            StatusMessage = $"{result.ProcessesTrimmed} processus optimisé(s), environ {ByteFormatter.Format(result.EstimatedBytesFreed)} libérés.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur pendant la libération de mémoire.");
            StatusMessage = "Erreur pendant la libération de mémoire.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanRun() => !IsBusy;

    partial void OnIsBusyChanged(bool value)
    {
        RunCommand.NotifyCanExecuteChanged();
        QuickCleanCommand.NotifyCanExecuteChanged();
        FreeMemoryCommand.NotifyCanExecuteChanged();
    }
}
