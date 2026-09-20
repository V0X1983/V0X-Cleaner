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
    private readonly IContextMenuManager _contextMenu;
    private readonly IWindowsServiceManager _services;
    private readonly ILogger<StartupManagerViewModel> _logger;

    /// <summary>Applications lancées au démarrage (registre Run et dossier Démarrage).</summary>
    public ObservableCollection<StartupEntryViewModel> Apps { get; } = [];

    /// <summary>Tâches planifiées déclenchées à l'ouverture de session.</summary>
    public ObservableCollection<StartupEntryViewModel> Tasks { get; } = [];

    /// <summary>Entrées du menu contextuel, regroupées par emplacement (dossier, fond, fichier).</summary>
    public ObservableCollection<ContextMenuGroupViewModel> ContextGroups { get; } = [];

    public ObservableCollection<ServiceEntryViewModel> Services { get; } = [];

    private int _contextCount;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Cliquez sur \"Actualiser\" pour lister les programmes de démarrage.";

    /// <summary>0 = applications, 1 = tâches planifiées, 2 = menu contextuel, 3 = services Windows.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAppsTab))]
    [NotifyPropertyChangedFor(nameof(IsTasksTab))]
    [NotifyPropertyChangedFor(nameof(IsContextTab))]
    [NotifyPropertyChangedFor(nameof(IsServicesTab))]
    [NotifyPropertyChangedFor(nameof(Headline))]
    [NotifyPropertyChangedFor(nameof(HeadlineDescription))]
    private int _currentTab;

    public bool IsAppsTab
    {
        get => CurrentTab == 0;
        set { if (value) { CurrentTab = 0; } }
    }

    public bool IsTasksTab
    {
        get => CurrentTab == 1;
        set { if (value) { CurrentTab = 1; } }
    }

    public bool IsContextTab
    {
        get => CurrentTab == 2;
        set { if (value) { CurrentTab = 2; } }
    }

    public bool IsServicesTab
    {
        get => CurrentTab == 3;
        set { if (value) { CurrentTab = 3; } }
    }

    public bool HasEntries => Apps.Count > 0 || Tasks.Count > 0 || ContextGroups.Count > 0 || Services.Count > 0;

    public string ContextTabTitle => $"Menu contextuel ({_contextCount})";

    public string ServicesTabTitle => $"Services Windows ({Services.Count})";

    public string AppsTabTitle => $"Applications s'ouvrant au démarrage ({Apps.Count})";

    public string TasksTabTitle => $"Tâches planifiées ({Tasks.Count})";

    public string Headline => CurrentTab switch
    {
        1 => "Gérer les tâches planifiées à l'ouverture de session",
        2 => "Simplifiez votre menu contextuel",
        3 => "Gérer les services Windows tiers",
        _ => "Gérer les applications qui se lancent au démarrage"
    };

    public string HeadlineDescription => CurrentTab switch
    {
        1 => "Ces tâches Windows se déclenchent à l'ouverture de session. Désactiver une tâche ne la supprime pas.",
        2 => "Supprimez les raccourcis superflus pour que votre menu contextuel reste organisé. Les entrées de Windows et de Microsoft ne sont pas listées.",
        3 => "Services installés par des logiciels tiers (les services Windows sont masqués). Désactiver un service l'empêche de démarrer avec Windows ; l'effet s'applique au prochain démarrage.",
        _ => "Choisissez les programmes qui démarrent automatiquement et activez ou désactivez-les facilement."
    };

    public StartupManagerViewModel(
        IStartupManager manager,
        IContextMenuManager contextMenu,
        IWindowsServiceManager services,
        ILogger<StartupManagerViewModel> logger)
    {
        _manager = manager;
        _contextMenu = contextMenu;
        _services = services;
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
            var viewModels = entries
                .Select(e => new StartupEntryViewModel(e, _manager))
                .OrderBy(v => v.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            Apps.Clear();
            Tasks.Clear();
            foreach (var viewModel in viewModels)
            {
                (viewModel.Entry.Source == StartupEntrySource.ScheduledTask ? Tasks : Apps).Add(viewModel);
            }

            var contextEntries = await _contextMenu.GetEntriesAsync();
            var contextViewModels = contextEntries.Select(e => new ContextMenuEntryViewModel(e, _contextMenu)).ToList();
            ContextGroups.Clear();
            foreach (var (scope, title, glyph) in new[]
            {
                (ContextMenuScope.Directory, "Répertoire", ""),
                (ContextMenuScope.Background, "Arrière-plan du bureau", ""),
                (ContextMenuScope.File, "Fichier", "")
            })
            {
                var group = contextViewModels.Where(v => v.Entry.Scope == scope).OrderBy(v => v.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
                if (group.Count > 0)
                {
                    ContextGroups.Add(new ContextMenuGroupViewModel(title, glyph, group));
                }
            }

            _contextCount = contextViewModels.Count;

            var services = await _services.GetServicesAsync();
            Services.Clear();
            foreach (var service in services)
            {
                Services.Add(new ServiceEntryViewModel(service, _services));
            }

            OnPropertyChanged(nameof(HasEntries));
            OnPropertyChanged(nameof(AppsTabTitle));
            OnPropertyChanged(nameof(TasksTabTitle));
            OnPropertyChanged(nameof(ContextTabTitle));
            OnPropertyChanged(nameof(ServicesTabTitle));
            StatusMessage = $"{viewModels.Count + contextViewModels.Count + services.Count} entrée(s) trouvée(s).";

            // Les éditeurs se lisent dans les fichiers : chargés en arrière-plan, l'affichage se complète au fil de l'eau.
            _ = Task.Run(() => Parallel.ForEach(viewModels, v => v.LoadPublisher()));
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

    partial void OnIsBusyChanged(bool value) => RefreshCommand.NotifyCanExecuteChanged();
}
