using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using V0XCleaner.Core.Abstractions;

namespace V0XCleaner.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public IReadOnlyList<NavigationItem> NavigationItems { get; } =
    [
        new NavigationItem { Key = "home", Title = "Accueil", Glyph = "" },
        new NavigationItem { Key = "cleaner", Title = "Nettoyeur", Glyph = "" },
        new NavigationItem { Key = "registry", Title = "Registre", Glyph = "" },
        new NavigationItem { Key = "tools", Title = "Outils", Glyph = "" },
        new NavigationItem { Key = "options", Title = "Options", Glyph = "" },
    ];

    [ObservableProperty]
    private NavigationItem _selectedNavigationItem;

    [ObservableProperty]
    private object _currentPage;

    private readonly Dictionary<string, object> _pageCache = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly IElevationService _elevationService;

    public string VersionLabel { get; } = "v" + (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0");

    public bool IsElevated => _elevationService.IsElevated;

    public string ElevationStatusLabel => IsElevated ? "Administrateur" : "Mode standard";

    public MainWindowViewModel(IServiceProvider serviceProvider, IElevationService elevationService)
    {
        _serviceProvider = serviceProvider;
        _elevationService = elevationService;
        _selectedNavigationItem = NavigationItems[0];
        _currentPage = GetOrCreatePage(_selectedNavigationItem.Key);
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
            "home" => (object)_serviceProvider.GetRequiredService<HomePageViewModel>(),
            "cleaner" => (object)_serviceProvider.GetRequiredService<CleanerPageViewModel>(),
            "registry" => (object)_serviceProvider.GetRequiredService<RegistryPageViewModel>(),
            "tools" => (object)_serviceProvider.GetRequiredService<ToolsPageViewModel>(),
            "options" => (object)_serviceProvider.GetRequiredService<OptionsViewModel>(),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };

        _pageCache[key] = page;
        return page;
    }
}
