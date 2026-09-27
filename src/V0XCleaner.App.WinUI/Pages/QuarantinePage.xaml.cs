using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using V0XCleaner.App.WinUI.Helpers;
using V0XCleaner.App.WinUI.ViewModels;

namespace V0XCleaner.App.WinUI.Pages;

public sealed partial class QuarantinePage : Page
{
    public QuarantineViewModel ViewModel { get; }

    public QuarantinePage()
    {
        ViewModel = App.Services.GetRequiredService<QuarantineViewModel>();
        InitializeComponent();
        DataContext = ViewModel;
    }

    private void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: QuarantineEntryViewModel entry })
        {
            ViewModel.RestoreCommand.Execute(entry);
        }
    }

    private void PurgeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: QuarantineEntryViewModel entry })
        {
            ViewModel.PurgeCommand.Execute(entry);
        }
    }

    private async void PurgeAllButton_Click(object sender, RoutedEventArgs e)
    {
        var confirmed = await DialogHelper.ConfirmAsync(
            XamlRoot,
            "Confirmer la suppression",
            "Supprimer définitivement tous les fichiers en quarantaine ?\n\nCette action est irréversible.",
            confirmText: "Supprimer");

        if (confirmed)
        {
            ViewModel.PurgeAllCommand.Execute(null);
        }
    }
}
