using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using V0XCleaner.App.WinUI.Helpers;
using V0XCleaner.App.WinUI.ViewModels;

namespace V0XCleaner.App.WinUI.Pages;

public sealed partial class DuplicateFinderPage : Page
{
    public DuplicateFinderViewModel ViewModel { get; }

    public DuplicateFinderPage()
    {
        ViewModel = App.Services.GetRequiredService<DuplicateFinderViewModel>();
        InitializeComponent();
        DataContext = ViewModel;
    }

    private async void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var folder = await PickerHelper.PickFolderAsync();
        if (folder is not null && Directory.Exists(folder))
        {
            ViewModel.FolderPath = folder;
        }
    }

    private async void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        var confirmed = await DialogHelper.ConfirmAsync(
            XamlRoot,
            "Confirmer la suppression",
            $"Supprimer définitivement les fichiers cochés ({ViewModel.FormattedSelected}) ?",
            confirmText: "Supprimer");

        if (confirmed)
        {
            await ViewModel.DeleteSelectedCommand.ExecuteAsync(null);
        }
    }
}
