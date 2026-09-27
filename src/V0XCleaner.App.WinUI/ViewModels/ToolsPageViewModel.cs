using CommunityToolkit.Mvvm.ComponentModel;

namespace V0XCleaner.App.WinUI.ViewModels;

/// <summary>
/// Coquille de sous-navigation de l'onglet "Outils". Contrairement à la version WPF (qui construit les
/// 10 ViewModels enfants d'avance et les sélectionne par DataTemplate DataType=), ici la page de contenu
/// est résolue à la demande par Frame.Navigate dans ToolsPage.xaml.cs (WinUI n'a pas d'équivalent direct
/// à la sélection de template implicite par type CLR).
/// </summary>
public partial class ToolsPageViewModel : ObservableObject
{
    public IReadOnlyList<NavigationItem> ToolItems { get; } =
    [
        new NavigationItem { Key = "startup", Title = "Démarrage", Glyph = "" },
        new NavigationItem { Key = "uninstall", Title = "Désinstalleur", Glyph = "" },
        new NavigationItem { Key = "disk-analyzer", Title = "Analyseur de disque", Glyph = "" },
        new NavigationItem { Key = "duplicates", Title = "Doublons", Glyph = "" },
        new NavigationItem { Key = "drive-wiper", Title = "Effaceur de disque", Glyph = "" },
        new NavigationItem { Key = "system-restore", Title = "Restauration système", Glyph = "" },
        new NavigationItem { Key = "quarantine", Title = "Corbeille de sécurité", Glyph = "" },
        new NavigationItem { Key = "software-updates", Title = "Mise à jour de logiciels", Glyph = "" },
        new NavigationItem { Key = "drivers", Title = "Mise à jour de pilotes", Glyph = "" },
        new NavigationItem { Key = "optimizer", Title = "Optimiseur de performances", Glyph = "" }
    ];

    [ObservableProperty]
    public partial NavigationItem SelectedToolItem { get; set; } = null!;
}
