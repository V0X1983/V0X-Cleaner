using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using V0XCleaner.App.WinUI.Helpers;
using V0XCleaner.App.WinUI.ViewModels;

namespace V0XCleaner.App.WinUI.Pages;

public sealed partial class HealthCheckPage : Microsoft.UI.Xaml.Controls.Page
{
    public HealthCheckViewModel ViewModel { get; }

    public HealthCheckPage()
    {
        ViewModel = App.Services.GetRequiredService<HealthCheckViewModel>();
        InitializeComponent();
        DataContext = ViewModel;
    }

    private async void CleanButton_Click(object sender, RoutedEventArgs e)
    {
        var confirmed = await DialogHelper.ConfirmAsync(
            XamlRoot,
            "Confirmer le nettoyage",
            $"Supprimer les éléments sélectionnés ({ViewModel.SelectionText}) ?\n\n" +
            "Les fichiers nettoyés passent par la Corbeille de sécurité (Outils > Corbeille de sécurité) et peuvent être restaurés, " +
            "sauf le contenu de la Corbeille Windows, vidé définitivement.",
            confirmText: "Nettoyer");

        if (confirmed)
        {
            await ViewModel.CleanCommand.ExecuteAsync(null);
        }
    }
}
