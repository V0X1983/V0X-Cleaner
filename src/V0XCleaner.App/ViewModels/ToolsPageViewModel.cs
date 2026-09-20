using CommunityToolkit.Mvvm.ComponentModel;

namespace V0XCleaner.App.ViewModels;

/// <summary>
/// Coquille de sous-navigation de l'onglet "Outils" : Démarrage (Étape 3), Désinstalleur
/// (Étape 4), Analyseur de disque, Doublons, Effaceur de disque et Restauration système (Étape 5).
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
        new NavigationItem { Key = "system-restore", Title = "Restauration système", Glyph = "" }
    ];

    [ObservableProperty]
    private NavigationItem _selectedToolItem;

    [ObservableProperty]
    private object _currentToolPage;

    private readonly StartupManagerViewModel _startupManagerViewModel;
    private readonly UninstallManagerViewModel _uninstallManagerViewModel;
    private readonly DiskAnalyzerViewModel _diskAnalyzerViewModel;
    private readonly DuplicateFinderViewModel _duplicateFinderViewModel;
    private readonly DriveWiperViewModel _driveWiperViewModel;
    private readonly SystemRestoreViewModel _systemRestoreViewModel;

    public ToolsPageViewModel(
        StartupManagerViewModel startupManagerViewModel,
        UninstallManagerViewModel uninstallManagerViewModel,
        DiskAnalyzerViewModel diskAnalyzerViewModel,
        DuplicateFinderViewModel duplicateFinderViewModel,
        DriveWiperViewModel driveWiperViewModel,
        SystemRestoreViewModel systemRestoreViewModel)
    {
        _startupManagerViewModel = startupManagerViewModel;
        _uninstallManagerViewModel = uninstallManagerViewModel;
        _diskAnalyzerViewModel = diskAnalyzerViewModel;
        _duplicateFinderViewModel = duplicateFinderViewModel;
        _driveWiperViewModel = driveWiperViewModel;
        _systemRestoreViewModel = systemRestoreViewModel;

        _selectedToolItem = ToolItems[0];
        _currentToolPage = _startupManagerViewModel;
    }

    partial void OnSelectedToolItemChanged(NavigationItem value)
    {
        CurrentToolPage = value.Key switch
        {
            "startup" => _startupManagerViewModel,
            "uninstall" => _uninstallManagerViewModel,
            "disk-analyzer" => _diskAnalyzerViewModel,
            "duplicates" => _duplicateFinderViewModel,
            "drive-wiper" => _driveWiperViewModel,
            "system-restore" => _systemRestoreViewModel,
            _ => CurrentToolPage
        };
    }
}
