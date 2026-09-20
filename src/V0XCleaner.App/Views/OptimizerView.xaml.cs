using System.Windows;
using System.Windows.Controls;
using V0XCleaner.App.ViewModels;

namespace V0XCleaner.App.Views;

public partial class OptimizerView : UserControl
{
    public OptimizerView()
    {
        InitializeComponent();
    }

    private async void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ProcessItemViewModel item } || DataContext is not OptimizerViewModel viewModel)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Fermer \"{item.Name}\" ?\n\nLes données non enregistrées de ce programme seront perdues s'il ne se ferme pas proprement.",
            "Confirmer la fermeture",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result == MessageBoxResult.Yes)
        {
            await viewModel.CloseCommand.ExecuteAsync(item);
        }
    }
}
