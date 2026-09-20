using CommunityToolkit.Mvvm.ComponentModel;

namespace V0XCleaner.App.ViewModels;

/// <summary>
/// Coquille de sous-navigation de l'onglet "Outils" : Bilan de santé, Démarrage (Étape 3),
/// Désinstalleur (Étape 4), Analyseur de disque, Doublons, Effaceur de disque et Restauration
/// système (Étape 5), Bilan de santé (Étape 6).
/// </summary>
public partial class ToolsPageViewModel : ObservableObject
{
    public IReadOnlyList<NavigationItem> ToolItems { get; } =
    [
        new NavigationItem { Key = "health-check", Title = "Bilan de santé", Glyph = "" },
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
    private NavigationItem _selectedToolItem;

    [ObservableProperty]
    private object _currentToolPage;

    private readonly HealthCheckViewModel _healthCheckViewModel;
    private readonly StartupManagerViewModel _startupManagerViewModel;
    private readonly UninstallManagerViewModel _uninstallManagerViewModel;
    private readonly DiskAnalyzerViewModel _diskAnalyzerViewModel;
    private readonly DuplicateFinderViewModel _duplicateFinderViewModel;
    private readonly DriveWiperViewModel _driveWiperViewModel;
    private readonly SystemRestoreViewModel _systemRestoreViewModel;
    private readonly QuarantineViewModel _quarantineViewModel;
    private readonly SoftwareUpdatesViewModel _softwareUpdatesViewModel;
    private readonly DriversViewModel _driversViewModel;
    private readonly OptimizerViewModel _optimizerViewModel;

    public ToolsPageViewModel(
        HealthCheckViewModel healthCheckViewModel,
        StartupManagerViewModel startupManagerViewModel,
        UninstallManagerViewModel uninstallManagerViewModel,
        DiskAnalyzerViewModel diskAnalyzerViewModel,
        DuplicateFinderViewModel duplicateFinderViewModel,
        DriveWiperViewModel driveWiperViewModel,
        SystemRestoreViewModel systemRestoreViewModel,
        QuarantineViewModel quarantineViewModel,
        SoftwareUpdatesViewModel softwareUpdatesViewModel,
        DriversViewModel driversViewModel,
        OptimizerViewModel optimizerViewModel)
    {
        _healthCheckViewModel = healthCheckViewModel;
        _startupManagerViewModel = startupManagerViewModel;
        _uninstallManagerViewModel = uninstallManagerViewModel;
        _diskAnalyzerViewModel = diskAnalyzerViewModel;
        _duplicateFinderViewModel = duplicateFinderViewModel;
        _driveWiperViewModel = driveWiperViewModel;
        _systemRestoreViewModel = systemRestoreViewModel;
        _quarantineViewModel = quarantineViewModel;
        _softwareUpdatesViewModel = softwareUpdatesViewModel;
        _driversViewModel = driversViewModel;
        _optimizerViewModel = optimizerViewModel;

        _selectedToolItem = ToolItems[0];
        _currentToolPage = _healthCheckViewModel;
    }

    partial void OnSelectedToolItemChanged(NavigationItem value)
    {
        CurrentToolPage = value.Key switch
        {
            "health-check" => _healthCheckViewModel,
            "startup" => _startupManagerViewModel,
            "uninstall" => _uninstallManagerViewModel,
            "disk-analyzer" => _diskAnalyzerViewModel,
            "duplicates" => _duplicateFinderViewModel,
            "drive-wiper" => _driveWiperViewModel,
            "system-restore" => _systemRestoreViewModel,
            "quarantine" => _quarantineViewModel,
            "software-updates" => _softwareUpdatesViewModel,
            "drivers" => _driversViewModel,
            "optimizer" => _optimizerViewModel,
            _ => CurrentToolPage
        };
    }
}
