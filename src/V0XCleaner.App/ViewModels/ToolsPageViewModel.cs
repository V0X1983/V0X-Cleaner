using CommunityToolkit.Mvvm.ComponentModel;

namespace V0XCleaner.App.ViewModels;

/// <summary>
/// Coquille de sous-navigation de l'onglet "Outils". Un seul outil pour l'instant (Démarrage,
/// Étape 3) ; le Désinstalleur, l'Analyseur de disque, les Doublons, l'Effaceur de disque et la
/// Restauration système (Étapes 4-5) viendront s'ajouter à ToolItems de la même façon.
/// </summary>
public partial class ToolsPageViewModel : ObservableObject
{
    public IReadOnlyList<NavigationItem> ToolItems { get; } =
    [
        new NavigationItem { Key = "startup", Title = "Démarrage", Glyph = "" }
    ];

    [ObservableProperty]
    private NavigationItem _selectedToolItem;

    [ObservableProperty]
    private object _currentToolPage;

    public ToolsPageViewModel(StartupManagerViewModel startupManagerViewModel)
    {
        _selectedToolItem = ToolItems[0];
        _currentToolPage = startupManagerViewModel;
    }
}
