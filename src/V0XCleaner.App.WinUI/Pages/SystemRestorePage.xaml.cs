using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using V0XCleaner.App.WinUI.Helpers;
using V0XCleaner.App.WinUI.ViewModels;

namespace V0XCleaner.App.WinUI.Pages;

public sealed partial class SystemRestorePage : Page
{
    public SystemRestoreViewModel ViewModel { get; }

    public SystemRestorePage()
    {
        ViewModel = App.Services.GetRequiredService<SystemRestoreViewModel>();
        InitializeComponent();
        DataContext = ViewModel;
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: RestorePointViewModel pointVm })
        {
            return;
        }

        var confirmed = await DialogHelper.ConfirmAsync(
            XamlRoot,
            "Confirmer la suppression",
            $"Supprimer le point de restauration « {pointVm.Description} » ({pointVm.FormattedCreationTime}) ?\n\nCette action est définitive.",
            confirmText: "Supprimer");

        if (confirmed)
        {
            await ViewModel.DeleteCommand.ExecuteAsync(pointVm);
        }
    }
}
