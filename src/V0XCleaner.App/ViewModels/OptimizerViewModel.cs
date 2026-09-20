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

    /// <summary>Impact estimé sur les performances d'après la mémoire utilisée : 1 = faible, 2 = moyen, 3 = élevé.</summary>
    public int ImpactLevel => Info.MemoryBytes >= 1024L * 1024 * 1024 ? 3 : Info.MemoryBytes >= 300L * 1024 * 1024 ? 2 : 1;

    public bool Dot2On => ImpactLevel >= 2;

    public bool Dot3On => ImpactLevel >= 3;

    public string ImpactText => ImpactLevel switch
    {
        3 => "Élevé",
        2 => "Moyen",
        _ => "Faible"
    };
}

/// <summary>Un nom de processus exclu de l'optimisation (onglet « exclues »).</summary>
public sealed class ExcludedProcessViewModel(string name)
{
    public string Name { get; } = name;
}

public partial class OptimizerViewModel : ObservableObject
{
    private readonly IProcessOptimizer _processes;
    private readonly IMemoryOptimizer _memory;
    private readonly ISettingsService _settings;
    private readonly ILogger<OptimizerViewModel> _logger;

    /// <summary>Applications actives, proposées à la mise en veille.</summary>
    public ObservableCollection<ProcessItemViewModel> Processes { get; } = [];

    public ObservableCollection<ProcessItemViewModel> Sleeping { get; } = [];

    public ObservableCollection<ExcludedProcessViewModel> Excluded { get; } = [];

    /// <summary>0 = actives, 1 = en veille, 2 = exclues.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsActiveTab))]
    [NotifyPropertyChangedFor(nameof(IsSleepingTab))]
    [NotifyPropertyChangedFor(nameof(IsExcludedTab))]
    [NotifyPropertyChangedFor(nameof(HeadlineCount))]
    [NotifyPropertyChangedFor(nameof(HeadlinePrefix))]
    [NotifyPropertyChangedFor(nameof(HeadlineSuffix))]
    [NotifyPropertyChangedFor(nameof(HeadlineDescription))]
    private int _currentTab;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Cliquez sur \"Analyser à nouveau\" pour lister les programmes les plus gourmands en mémoire.";

    [ObservableProperty]
    private string _lastAnalysisText = "Aucune analyse effectuée";

    public bool HasProcesses => Processes.Count > 0 || Sleeping.Count > 0;

    public bool IsActiveTab
    {
        get => CurrentTab == 0;
        set { if (value) { CurrentTab = 0; } }
    }

    public bool IsSleepingTab
    {
        get => CurrentTab == 1;
        set { if (value) { CurrentTab = 1; } }
    }

    public bool IsExcludedTab
    {
        get => CurrentTab == 2;
        set { if (value) { CurrentTab = 2; } }
    }

    public string ActiveTabTitle => $"Application(s) active(s) ({Processes.Count})";

    public string SleepingTabTitle => $"Application(s) en veille ({Sleeping.Count})";

    public string ExcludedTabTitle => $"Application(s) exclue(s) ({Excluded.Count})";

    private int CurrentCount => CurrentTab switch { 1 => Sleeping.Count, 2 => Excluded.Count, _ => Processes.Count };

    public string HeadlinePrefix => CurrentTab == 0 ? "Mettez " : string.Empty;

    public string HeadlineCount => CurrentCount > 1 ? $"{CurrentCount} applications" : $"{CurrentCount} application";

    public string HeadlineSuffix => CurrentTab switch
    {
        1 => " en veille",
        2 => CurrentCount > 1 ? " exclues" : " exclue",
        _ => " en mode veille"
    };

    public string HeadlineDescription => CurrentTab switch
    {
        1 => "Ces applications sont gelées : elles ne consomment plus de processeur. Réactivez-les quand vous en avez besoin ; elles sont aussi réactivées à la fermeture de V0X Cleaner.",
        2 => "Ces applications ne sont jamais proposées à la mise en veille.",
        _ => "Améliorez les performances de votre PC en mettant en veille les applications non essentielles."
    };

    public OptimizerViewModel(IProcessOptimizer processes, IMemoryOptimizer memory, ISettingsService settings, ILogger<OptimizerViewModel> logger)
    {
        _processes = processes;
        _memory = memory;
        _settings = settings;
        _logger = logger;
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            var excludedNames = _settings.Current.ExcludedProcessNames.ToList();
            var (active, sleeping) = await Task.Run(() =>
            {
                var sleepingList = _processes.GetSleepingProcesses();
                var sleepingIds = sleepingList.Select(p => p.Id).ToHashSet();
                var activeList = _processes.GetTopProcesses(40)
                    .Where(p => !sleepingIds.Contains(p.Id) && !excludedNames.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                    .Take(25)
                    .ToList();
                return (activeList, sleepingList);
            });

            Replace(Processes, active.Select(p => new ProcessItemViewModel(p)));
            Replace(Sleeping, sleeping.Select(p => new ProcessItemViewModel(p)));
            Replace(Excluded, excludedNames.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).Select(n => new ExcludedProcessViewModel(n)));

            StatusMessage = "Du plus gourmand au moins gourmand en mémoire. Les processus Windows sont masqués.";
            LastAnalysisText = $"Dernière analyse : {DateTime.Now:HH:mm}";
            RefreshCounts();
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
    private async Task SleepAsync(ProcessItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        var error = await RunBusyAsync(() => _processes.TrySuspend(item.Info.Id, out var e) ? null : e ?? "Échec.");
        await RefreshAsync();
        StatusMessage = error is null ? $"{item.Name} mis en veille." : $"Impossible de mettre {item.Name} en veille : {error}";
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ResumeAsync(ProcessItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        var error = await RunBusyAsync(() => _processes.TryResume(item.Info.Id, out var e) ? null : e ?? "Échec.");
        await RefreshAsync();
        StatusMessage = error is null ? $"{item.Name} réactivé." : $"Impossible de réactiver {item.Name} : {error}";
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task CloseAsync(ProcessItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        var error = await RunBusyAsync(() => _processes.TryClose(item.Info.Id, out var e) ? null : e ?? "Échec.");
        await RefreshAsync();
        StatusMessage = error is null ? $"{item.Name} fermé." : $"Impossible de fermer {item.Name} : {error}";
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ExcludeAsync(ProcessItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        var names = _settings.Current.ExcludedProcessNames;
        if (!names.Contains(item.Name, StringComparer.OrdinalIgnoreCase))
        {
            names.Add(item.Name);
            await _settings.SaveAsync();
        }

        await RefreshAsync();
        StatusMessage = $"{item.Name} ne sera plus proposé à la mise en veille.";
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task IncludeAsync(ExcludedProcessViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        _settings.Current.ExcludedProcessNames.RemoveAll(n => n.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
        await _settings.SaveAsync();
        await RefreshAsync();
        StatusMessage = $"{item.Name} peut de nouveau être mis en veille.";
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task FreeMemoryAsync()
    {
        IsBusy = true;
        string message;
        try
        {
            var result = await Task.Run(() => _memory.FreeMemoryAsync());
            message = $"Mémoire libérée : environ {ByteFormatter.Format(result.EstimatedBytesFreed)} ({result.ProcessesTrimmed} processus).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la libération de la RAM.");
            message = "Erreur lors de la libération de la RAM.";
        }
        finally
        {
            IsBusy = false;
        }

        await RefreshAsync();
        StatusMessage = message;
    }

    private async Task<string?> RunBusyAsync(Func<string?> action)
    {
        IsBusy = true;
        try
        {
            return await Task.Run(action);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }

    private void RefreshCounts()
    {
        OnPropertyChanged(nameof(HasProcesses));
        OnPropertyChanged(nameof(ActiveTabTitle));
        OnPropertyChanged(nameof(SleepingTabTitle));
        OnPropertyChanged(nameof(ExcludedTabTitle));
        OnPropertyChanged(nameof(HeadlineCount));
        OnPropertyChanged(nameof(HeadlineSuffix));
    }

    private bool CanRun() => !IsBusy;

    partial void OnIsBusyChanged(bool value)
    {
        RefreshCommand.NotifyCanExecuteChanged();
        SleepCommand.NotifyCanExecuteChanged();
        ResumeCommand.NotifyCanExecuteChanged();
        CloseCommand.NotifyCanExecuteChanged();
        ExcludeCommand.NotifyCanExecuteChanged();
        IncludeCommand.NotifyCanExecuteChanged();
        FreeMemoryCommand.NotifyCanExecuteChanged();
    }
}
