using System.Windows;
using System.Windows.Controls;
using V0XCleaner.App.ViewModels;

namespace V0XCleaner.App.Views;

public partial class RegistryPageView : UserControl
{
    public RegistryPageView()
    {
        InitializeComponent();
    }

    private async void RepairButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not RegistryPageViewModel viewModel)
        {
            return;
        }

        if (!viewModel.SimulationMode)
        {
            var result = MessageBox.Show(
                $"Ceci va modifier le registre Windows pour réparer {viewModel.TotalIssuesFound} élément(s) sélectionné(s).\n\n" +
                "Une sauvegarde .reg sera créée automatiquement avant toute modification. Continuer ?",
                "Confirmer la réparation du registre",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        await viewModel.RepairCommand.ExecuteAsync(null);
    }

    private async void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not RegistryPageViewModel viewModel || viewModel.SelectedBackup is null)
        {
            return;
        }

        var fileName = System.IO.Path.GetFileName(viewModel.SelectedBackup);
        var result = MessageBox.Show(
            $"Ceci va réimporter la sauvegarde \"{fileName}\" dans le registre Windows, restaurant les clés telles qu'elles étaient au moment de cette sauvegarde.\n\nContinuer ?",
            "Confirmer la restauration",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        await viewModel.RestoreBackupCommand.ExecuteAsync(null);
    }
}
