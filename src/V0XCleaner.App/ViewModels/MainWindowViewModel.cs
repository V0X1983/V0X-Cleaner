using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using V0XCleaner.Core;
using V0XCleaner.Core.Abstractions;

namespace V0XCleaner.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public IReadOnlyList<NavigationItem> NavigationItems { get; } =
    [
        new NavigationItem { Key = "health-check", Title = "Bilan de santé", Glyph = "" },
        new NavigationItem { Key = "cleaner", Title = "Nettoyeur", Glyph = "" },
        new NavigationItem { Key = "registry", Title = "Registre", Glyph = "" },
        new NavigationItem { Key = "tools", Title = "Outils", Glyph = "" },
        new NavigationItem { Key = "options", Title = "Options", Glyph = "" },
        new NavigationItem { Key = "help", Title = "Aide", Glyph = "" },
    ];

    [ObservableProperty]
    private NavigationItem _selectedNavigationItem;

    [ObservableProperty]
    private object _currentPage;

    private readonly Dictionary<string, object> _pageCache = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly IElevationService _elevationService;
    private readonly IUpdateChecker _updateChecker;
    private readonly ISettingsService _settings;
    private readonly ILogger<MainWindowViewModel> _logger;

    public string VersionLabel { get; } = "v" + (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0");

    public bool IsElevated => _elevationService.IsElevated;

    public string ElevationStatusLabel => IsElevated ? "Administrateur" : "Mode standard";

    /// <summary>Non nul quand une version plus récente a été trouvée au démarrage (vérification silencieuse, lecture seule).</summary>
    [ObservableProperty]
    private string? _updateAvailableLabel;

    [ObservableProperty]
    private string? _updateAvailableUrl;

    public MainWindowViewModel(IServiceProvider serviceProvider, IElevationService elevationService,
        IUpdateChecker updateChecker, ISettingsService settings, ILogger<MainWindowViewModel> logger)
    {
        _serviceProvider = serviceProvider;
        _elevationService = elevationService;
        _updateChecker = updateChecker;
        _settings = settings;
        _logger = logger;
        _selectedNavigationItem = NavigationItems[0];
        _currentPage = GetOrCreatePage(_selectedNavigationItem.Key);
        _ = CheckForUpdateSilentlyAsync();
    }

    /// <summary>
    /// Vérification automatique au démarrage : lecture seule (contrairement à « Vérifier maintenant » dans
    /// Options, qui télécharge et installe). Se contente de signaler qu'une version existe, sans jamais
    /// télécharger ni installer quoi que ce soit sans action explicite de l'utilisateur.
    /// </summary>
    private async Task CheckForUpdateSilentlyAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
            var current = _settings.Current;
            var result = await _updateChecker.GetLatestReleaseAsync(current.UpdateCheckOwner, current.UpdateCheckRepo);
            var installed = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (result.Success && UpdateVersionComparer.IsNewer(result.Version, installed))
            {
                UpdateAvailableLabel = $"Nouvelle version {result.Version} disponible";
                UpdateAvailableUrl = result.ReleaseUrl;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vérification automatique des mises à jour au démarrage impossible.");
        }
    }

    [RelayCommand(CanExecute = nameof(CanRelaunchElevated))]
    private void RelaunchElevated()
    {
        if (_elevationService.RelaunchElevated())
        {
            System.Windows.Application.Current?.Shutdown();
        }
    }

    private bool CanRelaunchElevated() => !IsElevated;

    partial void OnSelectedNavigationItemChanged(NavigationItem value)
    {
        CurrentPage = GetOrCreatePage(value.Key);
    }

    private object GetOrCreatePage(string key)
    {
        if (_pageCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var page = key switch
        {
            "health-check" => (object)_serviceProvider.GetRequiredService<HealthCheckViewModel>(),
            "cleaner" => (object)_serviceProvider.GetRequiredService<CleanerPageViewModel>(),
            "registry" => (object)_serviceProvider.GetRequiredService<RegistryPageViewModel>(),
            "tools" => (object)_serviceProvider.GetRequiredService<ToolsPageViewModel>(),
            "options" => (object)_serviceProvider.GetRequiredService<OptionsViewModel>(),
            "help" => (object)_serviceProvider.GetRequiredService<HelpViewModel>(),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };

        _pageCache[key] = page;
        return page;
    }
}
