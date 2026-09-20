using System.Windows;
using System.Windows.Controls;
using V0XCleaner.App.ViewModels;

namespace V0XCleaner.App.Views;

public partial class DiskAnalyzerView : UserControl
{
    public DiskAnalyzerView()
    {
        InitializeComponent();
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: FolderSizeNodeViewModel nodeVm } || DataContext is not DiskAnalyzerViewModel viewModel)
        {
            return;
        }

        var kind = nodeVm.IsFile ? "le fichier" : "le dossier (et tout son contenu)";
        var result = MessageBox.Show(
            $"Supprimer définitivement {kind} \"{nodeVm.Name}\" ({nodeVm.FormattedSize}) ?",
            "Confirmer la suppression",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        await viewModel.DeleteNodeCommand.ExecuteAsync(nodeVm);
    }
}
