using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using V0XCleaner.App.ViewModels;

namespace V0XCleaner.App.Views;

public partial class DuplicateFinderView : UserControl
{
    public DuplicateFinderView()
    {
        InitializeComponent();
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not DuplicateFinderViewModel viewModel)
        {
            return;
        }

        var dialog = new OpenFolderDialog
        {
            Title = "Choisir un dossier à analyser",
            InitialDirectory = Directory.Exists(viewModel.FolderPath) ? viewModel.FolderPath : null
        };

        if (dialog.ShowDialog() == true)
        {
            viewModel.FolderPath = dialog.FolderName;
        }
    }

    private async void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not DuplicateFinderViewModel viewModel)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Supprimer définitivement les fichiers cochés ({viewModel.FormattedSelected}) ?",
            "Confirmer la suppression",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        await viewModel.DeleteSelectedCommand.ExecuteAsync(null);
    }
}
