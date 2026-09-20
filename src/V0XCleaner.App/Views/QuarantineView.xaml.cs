using System.Windows;
using System.Windows.Controls;
using V0XCleaner.App.ViewModels;

namespace V0XCleaner.App.Views;

public partial class QuarantineView : UserControl
{
    public QuarantineView()
    {
        InitializeComponent();
    }

    private void PurgeAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not QuarantineViewModel viewModel)
        {
            return;
        }

        var result = MessageBox.Show(
            "Supprimer définitivement tous les fichiers en quarantaine ?\n\nCette action est irréversible.",
            "Confirmer la suppression",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result == MessageBoxResult.Yes)
        {
            viewModel.PurgeAllCommand.Execute(null);
        }
    }
}
