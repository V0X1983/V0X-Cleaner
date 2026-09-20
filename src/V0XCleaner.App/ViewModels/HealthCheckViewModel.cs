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

    public ScanProgress Loading { get; } = new();

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        IsBusy = true;
        StatusMessage = "Analyse en cours...";
        var ct = Loading.Begin("Analyse de votre PC en cours...");

        try
        {
            var result = await Task.Run(() => _healthCheck.RunAsync(ct), ct);
            OverallScore = result.OverallScore;
            Items.Clear();
            foreach (var item in result.Items)
            {
                Items.Add(new HealthCheckItemViewModel(item));
            }

            StatusMessage = $"Bilan terminé — score global : {OverallScore}/100.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Analyse arrêtée.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur pendant le bilan de santé.");
            StatusMessage = "Erreur pendant le bilan de santé.";
        }
        finally
        {
            Loading.End();
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task QuickCleanAsync()
    {
        IsBusy = true;
        StatusMessage = "Nettoyage rapide en cours...";
        var ct = Loading.Begin("Nettoyage rapide en cours...", "Arrêter le nettoyage");

        try
        {
            long freed = 0;
            var tasks = _cleaningCatalog.GetTasks().Where(t => t.SelectedByDefault && t.Section == CleaningSection.System).ToList();
            var done = 0;
            foreach (var task in tasks)
            {
                Loading.Message = $"Nettoyage : {task.DisplayName}";
                var scan = await Task.Run(() => task.Scanner.ScanAsync(ct), ct);
                if (scan.Items.Count > 0)
                {
                    var result = await Task.Run(() => task.Cleaner.CleanAsync(scan.Items, OperationMode.Execute, ct), ct);
                    freed += result.FreedBytes;
                }

                done++;
                Loading.Progress = done * 100.0 / tasks.Count;
            }

            StatusMessage = $"Nettoyage rapide terminé : {ByteFormatter.Format(freed)} libérés.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Nettoyage arrêté.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur pendant le nettoyage rapide.");
            StatusMessage = "Erreur pendant le nettoyage rapide.";
        }
        finally
        {
            Loading.End();
            IsBusy = false;
        }

        await RunAsync();
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task FreeMemoryAsync()
    {
        IsBusy = true;
        StatusMessage = "Libération de la mémoire en cours...";
        var ct = Loading.Begin("Libération de la mémoire en cours...", "Arrêter");

        try
        {
            var result = await Task.Run(() => _memoryOptimizer.FreeMemoryAsync(ct), ct);
            StatusMessage = $"{result.ProcessesTrimmed} processus optimisé(s), environ {ByteFormatter.Format(result.EstimatedBytesFreed)} libérés.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Libération de la mémoire arrêtée.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur pendant la libération de mémoire.");
            StatusMessage = "Erreur pendant la libération de mémoire.";
        }
        finally
        {
            Loading.End();
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
