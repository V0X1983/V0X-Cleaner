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
        new NavigationItem { Key = "updates", Title = "Mises à jour", Glyph = "" },
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
            "tools" => new PlaceholderPageViewModel
            {
                Title = "Outils",
                Description = "Démarrage, désinstalleur, analyseur de disque, doublons, effaceur de disque, restauration système : Étapes 3 à 5."
            },
            "options" => new PlaceholderPageViewModel
            {
                Title = "Options",
                Description = "Exclusions, langue, démarrage avec Windows, thème, planification : Étape 8."
            },
            "updates" => new PlaceholderPageViewModel
            {
                Title = "Mises à jour",
                Description = "Vérification et mise à jour automatique de l'application et de ses définitions : Étape 6."
            },
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };

        _pageCache[key] = page;
        return page;
    }
}
