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

    private async void QuickCleanButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not HealthCheckViewModel viewModel)
        {
            return;
        }

        var result = MessageBox.Show(
            "Nettoyer automatiquement les fichiers temporaires système par défaut (sans revue détaillée) ?\n\n" +
            "Pour un contrôle précis catégorie par catégorie, utilisez plutôt l'onglet Nettoyeur.",
            "Confirmer le nettoyage rapide",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        await viewModel.QuickCleanCommand.ExecuteAsync(null);
    }
}
