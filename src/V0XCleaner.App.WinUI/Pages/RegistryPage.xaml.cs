using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using V0XCleaner.App.WinUI.Helpers;
using V0XCleaner.App.WinUI.ViewModels;

namespace V0XCleaner.App.WinUI.Pages;

public sealed partial class RegistryPage : Page
{
    public RegistryPageViewModel ViewModel { get; }

    public RegistryPage()
    {
        ViewModel = App.Services.GetRequiredService<RegistryPageViewModel>();
        InitializeComponent();
        DataContext = ViewModel;
    }

    private async void RepairButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.SimulationMode)
        {
            var confirmed = await DialogHelper.ConfirmAsync(
                XamlRoot,
                "Confirmer la réparation du registre",
                $"Ceci va modifier le registre Windows pour réparer {ViewModel.TotalIssuesFound} élément(s) sélectionné(s).\n\n" +
                "Une sauvegarde .reg sera créée automatiquement avant toute modification. Continuer ?",
                confirmText: "Réparer");

            if (!confirmed)
            {
                return;
            }
        }

        await ViewModel.RepairCommand.ExecuteAsync(null);
    }

    private async void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedBackup is null)
        {
            return;
        }

        var fileName = Path.GetFileName(ViewModel.SelectedBackup);
        var confirmed = await DialogHelper.ConfirmAsync(
            XamlRoot,
            "Confirmer la restauration",
            $"Ceci va réimporter la sauvegarde « {fileName} » dans le registre Windows, restaurant les clés telles qu'elles étaient au moment de cette sauvegarde.\n\nContinuer ?",
            confirmText: "Restaurer");

        if (!confirmed)
        {
            return;
        }

        await ViewModel.RestoreBackupCommand.ExecuteAsync(null);
    }
}
