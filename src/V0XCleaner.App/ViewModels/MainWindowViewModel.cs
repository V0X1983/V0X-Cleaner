using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace V0XCleaner.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public IReadOnlyList<NavigationItem> NavigationItems { get; } =
    [
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

    public MainWindowViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _selectedNavigationItem = NavigationItems[0];
        _currentPage = GetOrCreatePage(_selectedNavigationItem.Key);
    }

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
