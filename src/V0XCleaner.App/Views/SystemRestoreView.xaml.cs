using System.Windows;
using System.Windows.Controls;
using V0XCleaner.App.ViewModels;

namespace V0XCleaner.App.Views;

public partial class SystemRestoreView : UserControl
{
    public SystemRestoreView()
    {
        InitializeComponent();
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: RestorePointViewModel pointVm } || DataContext is not SystemRestoreViewModel viewModel)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Supprimer le point de restauration \"{pointVm.Description}\" ({pointVm.FormattedCreationTime}) ?\n\n" +
            "Cette action est définitive.",
            "Confirmer la suppression",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        await viewModel.DeleteCommand.ExecuteAsync(pointVm);
    }
}
