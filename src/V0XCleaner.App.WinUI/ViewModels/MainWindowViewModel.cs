using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    public string VersionLabel { get; } =
        "v" + (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0");

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

    public MainWindowViewModel(IElevatedOperationClient elevatedClient)
    {
        _elevatedClient = elevatedClient;
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
