using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using V0XCleaner.App.WinUI.Helpers;
using V0XCleaner.App.WinUI.ViewModels;

namespace V0XCleaner.App.WinUI.Pages;

public sealed partial class CleanerPage : Page
{
    public CleanerPageViewModel ViewModel { get; }

    public CleanerPage()
    {
        ViewModel = App.Services.GetRequiredService<CleanerPageViewModel>();
        InitializeComponent();
        DataContext = ViewModel;
    }

    private async void CleanButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.SimulationMode)
        {
            var confirmed = await DialogHelper.ConfirmAsync(
                XamlRoot,
                "Confirmer le nettoyage",
                $"Ceci va supprimer définitivement les éléments sélectionnés ({ViewModel.FormattedTotalRecoverable}).\n\nContinuer ?",
                confirmText: "Nettoyer");

            if (!confirmed)
            {
                return;
            }
        }

        await ViewModel.CleanCommand.ExecuteAsync(null);
    }
}
