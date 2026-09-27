using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using V0XCleaner.App.WinUI.Helpers;
using V0XCleaner.App.WinUI.ViewModels;

namespace V0XCleaner.App.WinUI.Pages;

public sealed partial class DriversPage : Page
{
    public DriversViewModel ViewModel { get; }

    public DriversPage()
    {
        ViewModel = App.Services.GetRequiredService<DriversViewModel>();
        InitializeComponent();
        DataContext = ViewModel;
    }

    private void RowActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: DriverRowViewModel row })
        {
            ViewModel.RowActionCommand.Execute(row);
        }
    }

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        var count = ViewModel.SelectedIds().Count;
        var adminNote = ViewModel.IsElevated
            ? string.Empty
            : "\n\nV0X Cleaner n'est pas lancé en administrateur : l'installation échouera probablement. Utilisez d'abord « Relancer en administrateur ».";

        var confirmed = await DialogHelper.ConfirmAsync(
            XamlRoot,
            "Confirmer l'installation",
            $"Installer {count} pilote(s) via Windows Update ?\n\n" +
            "Seuls des pilotes signés par Microsoft sont installés. Un mauvais pilote peut néanmoins causer des instabilités : " +
            "créez un point de restauration (Outils › Restauration système) avant de continuer." + adminNote,
            confirmText: "Installer");

        if (confirmed)
        {
            await ViewModel.InstallSelectedAsync();
        }
    }
}
