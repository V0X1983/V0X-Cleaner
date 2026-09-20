using System.Windows;
using System.Windows.Controls;
using V0XCleaner.App.ViewModels;

namespace V0XCleaner.App.Views;

public partial class DriversView : UserControl
{
    public DriversView()
    {
        InitializeComponent();
    }

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not DriversViewModel viewModel)
        {
            return;
        }

        var count = viewModel.SelectedIds().Count;
        var adminNote = viewModel.IsElevated
            ? string.Empty
            : "\n\nV0X Cleaner n'est pas lancé en administrateur : l'installation échouera probablement. Utilisez d'abord « Relancer en administrateur ».";

        var result = MessageBox.Show(
            $"Installer {count} pilote(s) via Windows Update ?\n\n" +
            "Seuls des pilotes signés par Microsoft sont installés. Un mauvais pilote peut néanmoins causer des instabilités : " +
            "créez un point de restauration (Outils › Restauration système) avant de continuer." + adminNote,
            "Confirmer l'installation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result == MessageBoxResult.Yes)
        {
            await viewModel.InstallSelectedAsync();
        }
    }
}
