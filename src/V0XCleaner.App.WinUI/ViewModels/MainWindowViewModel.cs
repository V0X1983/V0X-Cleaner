using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using V0XCleaner.Core;
using V0XCleaner.Core.Abstractions;

namespace V0XCleaner.App.WinUI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public IReadOnlyList<NavigationItem> NavigationItems { get; } =
    [
        new NavigationItem { Key = "health-check", Title = "Bilan de santé", Glyph = "" },
        new NavigationItem { Key = "cleaner", Title = "Nettoyeur", Glyph = "" },
        new NavigationItem { Key = "registry", Title = "Registre", Glyph = "" },
        new NavigationItem { Key = "tools", Title = "Outils", Glyph = "" },
        new NavigationItem { Key = "options", Title = "Options", Glyph = "" },
        new NavigationItem { Key = "help", Title = "Aide", Glyph = "" },
    ];

    private readonly IElevatedOperationClient _elevatedClient;
    private readonly IUpdateChecker _updateChecker;
    private readonly ISettingsService _settings;
    private readonly ILogger<MainWindowViewModel> _logger;

    public string VersionLabel { get; } =
        "v" + (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0");

    /// <summary>Non nul quand une version plus récente a été trouvée au démarrage (vérification silencieuse, lecture seule).</summary>
    [ObservableProperty]
    public partial string? UpdateAvailableLabel { get; set; }

    [ObservableProperty]
    public partial string? UpdateAvailableUrl { get; set; }

    /// <summary>
    /// Contrairement à l'ancienne app WPF (élévation de tout le process via IElevationService),
    /// cette app packagée MSIX reste toujours asInvoker : "élevé" signifie ici que le helper
    /// V0XCleaner.ElevatedHelper a déjà répondu avec succès une fois cette session (voir
    /// ProcessElevatedOperationClient). Tant que ce n'est pas vérifié, le bouton lance un aller-
    /// retour à vide (aucune opération registre) juste pour confirmer que l'invite UAC fonctionne.
    /// </summary>
    [ObservableProperty]
    public partial bool IsHelperElevated { get; set; }

    [ObservableProperty]
    public partial string ElevationStatusLabel { get; set; } = "Élévation non vérifiée";

    public MainWindowViewModel(IElevatedOperationClient elevatedClient, IUpdateChecker updateChecker,
        ISettingsService settings, ILogger<MainWindowViewModel> logger)
    {
        _elevatedClient = elevatedClient;
        _updateChecker = updateChecker;
        _settings = settings;
        _logger = logger;
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

    [RelayCommand(CanExecute = nameof(CanCheckElevation))]
    private async Task CheckElevationAsync()
    {
        ElevationStatusLabel = "Vérification en cours…";

        var response = await _elevatedClient.ExecuteAsync([]);

        if (response.Success)
        {
            IsHelperElevated = true;
            ElevationStatusLabel = "Élévation confirmée";
        }
        else
        {
            IsHelperElevated = false;
            ElevationStatusLabel = response.ErrorMessage ?? "Élévation refusée";
        }

        CheckElevationCommand.NotifyCanExecuteChanged();
    }

    private bool CanCheckElevation() => !IsHelperElevated;
}
