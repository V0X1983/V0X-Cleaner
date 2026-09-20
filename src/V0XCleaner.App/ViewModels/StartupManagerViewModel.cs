using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.App.ViewModels;

public partial class StartupManagerViewModel : ObservableObject
{
    private readonly IStartupManager _manager;
    private readonly ILogger<StartupManagerViewModel> _logger;

    public ObservableCollection<StartupGroupViewModel> Groups { get; } = [];

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Cliquez sur \"Actualiser\" pour lister les programmes de démarrage.";

    public StartupManagerViewModel(IStartupManager manager, ILogger<StartupManagerViewModel> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        StatusMessage = "Chargement des programmes de démarrage...";

        try
        {
            var entries = await Task.Run(() => _manager.GetEntriesAsync());

            Groups.Clear();
            var entryViewModels = entries.Select(e => new StartupEntryViewModel(e, _manager)).ToList();

            foreach (var group in entryViewModels.GroupBy(v => v.Entry.Source))
            {
                Groups.Add(new StartupGroupViewModel(GroupTitle(group.Key), group.ToList()));
            }

            StatusMessage = $"{entryViewModels.Count} entrée(s) trouvée(s).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du chargement des entrées de démarrage.");
            StatusMessage = "Erreur lors du chargement des entrées de démarrage.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteEntryAsync(StartupEntryViewModel? entryVm)
    {
        if (entryVm is null)
        {
            return;
        }

        var ok = await entryVm.DeleteAsync();
        if (ok)
        {
            await RefreshAsync();
        }
    }

    private bool CanRun() => !IsBusy;

    private static string GroupTitle(StartupEntrySource source) => source switch
    {
        StartupEntrySource.RegistryRun => "Registre (Run)",
        StartupEntrySource.StartupFolder => "Dossier Démarrage",
        StartupEntrySource.ScheduledTask => "Tâches planifiées (à l'ouverture de session)",
        _ => source.ToString()
    };

    partial void OnIsBusyChanged(bool value) => RefreshCommand.NotifyCanExecuteChanged();
}
