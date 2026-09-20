using System.Windows;
using System.Windows.Controls;
using V0XCleaner.App.ViewModels;

namespace V0XCleaner.App.Views;

public partial class HealthCheckView : UserControl
{
    public HealthCheckView()
    {
        InitializeComponent();
    }

    private async void CleanButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not HealthCheckViewModel viewModel)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Supprimer les éléments sélectionnés ({viewModel.SelectionText}) ?\n\n" +
            "Les fichiers nettoyés passent par la Corbeille de sécurité (Outils > Corbeille de sécurité) et peuvent être restaurés, " +
            "sauf le contenu de la Corbeille Windows, vidé définitivement.",
            "Confirmer le nettoyage",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        await viewModel.CleanCommand.ExecuteAsync(null);
    }
}
