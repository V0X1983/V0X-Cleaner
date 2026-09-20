using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using V0XCleaner.App.Helpers;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.App.ViewModels;

public sealed class ProcessItemViewModel(RunningProcessInfo info)
{
    public RunningProcessInfo Info { get; } = info;

    public string Name => Info.Name;

    public string MemoryText => ByteFormatter.Format(Info.MemoryBytes);

    public string KindText => Info.HasWindow ? "Application" : "Arrière-plan";
}

public partial class OptimizerViewModel : ObservableObject
{
    private readonly IProcessOptimizer _processes;
    private readonly IMemoryOptimizer _memory;
    private readonly ILogger<OptimizerViewModel> _logger;

    public ObservableCollection<ProcessItemViewModel> Processes { get; } = [];

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Cliquez sur \"Actualiser\" pour lister les programmes les plus gourmands en mémoire.";

    public OptimizerViewModel(IProcessOptimizer processes, IMemoryOptimizer memory, ILogger<OptimizerViewModel> logger)
    {
        _processes = processes;
        _memory = memory;
        _logger = logger;
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            var list = await Task.Run(() => _processes.GetTopProcesses(25));
            Processes.Clear();
            foreach (var process in list)
            {
                Processes.Add(new ProcessItemViewModel(process));
            }

            StatusMessage = $"{Processes.Count} programme(s) affiché(s), du plus gourmand au moins gourmand. Les processus Windows sont masqués.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la lecture des processus.");
            StatusMessage = "Erreur lors de la lecture des processus.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task CloseAsync(ProcessItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var ok = await Task.Run(() => _processes.TryClose(item.Info.Id, out var error) ? null : error ?? "Échec.");
            StatusMessage = ok is null ? $"{item.Name} fermé." : $"Impossible de fermer {item.Name} : {ok}";
        }
        finally
        {
            IsBusy = false;
        }

        await RefreshAsync();
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task FreeMemoryAsync()
    {
        IsBusy = true;
        try
        {
            var result = await Task.Run(() => _memory.FreeMemoryAsync());
            StatusMessage = $"Mémoire libérée : environ {ByteFormatter.Format(result.EstimatedBytesFreed)} ({result.ProcessesTrimmed} processus).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la libération de la RAM.");
            StatusMessage = "Erreur lors de la libération de la RAM.";
        }
        finally
        {
            IsBusy = false;
        }

        await RefreshAsync();
    }

    private bool CanRun() => !IsBusy;

    partial void OnIsBusyChanged(bool value)
    {
        RefreshCommand.NotifyCanExecuteChanged();
        CloseCommand.NotifyCanExecuteChanged();
        FreeMemoryCommand.NotifyCanExecuteChanged();
    }
}
