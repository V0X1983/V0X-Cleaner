using CommunityToolkit.Mvvm.ComponentModel;

namespace V0XCleaner.App.ViewModels;

/// <summary>
/// Coquille de sous-navigation de l'onglet "Outils" : Démarrage (Étape 3) et Désinstalleur
/// (Étape 4) pour l'instant. Analyseur de disque, Doublons et Effaceur de disque (Étape 5)
/// viendront s'ajouter à ToolItems de la même façon.
/// </summary>
public partial class ToolsPageViewModel : ObservableObject
{
    public IReadOnlyList<NavigationItem> ToolItems { get; } =
    [
        new NavigationItem { Key = "startup", Title = "Démarrage", Glyph = "" },
        new NavigationItem { Key = "uninstall", Title = "Désinstalleur", Glyph = "" }
    ];

    [ObservableProperty]
    private NavigationItem _selectedToolItem;

    [ObservableProperty]
    private object _currentToolPage;

    private readonly StartupManagerViewModel _startupManagerViewModel;
    private readonly UninstallManagerViewModel _uninstallManagerViewModel;

    public ToolsPageViewModel(StartupManagerViewModel startupManagerViewModel, UninstallManagerViewModel uninstallManagerViewModel)
    {
        _startupManagerViewModel = startupManagerViewModel;
        _uninstallManagerViewModel = uninstallManagerViewModel;
        _selectedToolItem = ToolItems[0];
        _currentToolPage = _startupManagerViewModel;
    }

    partial void OnSelectedToolItemChanged(NavigationItem value)
    {
        CurrentToolPage = value.Key switch
        {
            "startup" => _startupManagerViewModel,
            "uninstall" => _uninstallManagerViewModel,
            _ => CurrentToolPage
        };
    }
}
