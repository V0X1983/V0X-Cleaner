using System.Windows;
using System.Windows.Controls;
using V0XCleaner.App.ViewModels;

namespace V0XCleaner.App.Views;

public partial class CleanerPageView : UserControl
{
    public CleanerPageView()
    {
        InitializeComponent();
    }

    private async void CleanButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not CleanerPageViewModel viewModel)
        {
            return;
        }

        if (!viewModel.SimulationMode)
        {
            var result = MessageBox.Show(
                $"Ceci va supprimer définitivement les éléments sélectionnés ({viewModel.FormattedTotalRecoverable}).\n\nContinuer ?",
                "Confirmer le nettoyage",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        await viewModel.CleanCommand.ExecuteAsync(null);
    }
}
