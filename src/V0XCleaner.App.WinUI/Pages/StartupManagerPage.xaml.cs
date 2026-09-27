using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using V0XCleaner.App.WinUI.Helpers;
using V0XCleaner.App.WinUI.ViewModels;

namespace V0XCleaner.App.WinUI.Pages;

public sealed partial class StartupManagerPage : Page
{
    public StartupManagerViewModel ViewModel { get; }

    public StartupManagerPage()
    {
        ViewModel = App.Services.GetRequiredService<StartupManagerViewModel>();
        InitializeComponent();
        DataContext = ViewModel;

        Loaded += (_, _) =>
        {
            if (!ViewModel.HasEntries && ViewModel.RefreshCommand.CanExecute(null))
            {
                ViewModel.RefreshCommand.Execute(null);
            }
        };
    }

    /// <summary>Menu « ... » d'une ligne : suppression de l'entrée, après confirmation.</summary>
    private void MoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: StartupEntryViewModel entry } button)
        {
            return;
        }

        var menu = new MenuFlyout();
        var delete = new MenuFlyoutItem { Text = "Supprimer l'entrée" };
        delete.Click += async (_, _) =>
        {
            var confirmed = await DialogHelper.ConfirmAsync(
                XamlRoot,
                "Confirmer la suppression",
                $"Supprimer définitivement « {entry.Name} » du démarrage ?\n\n" +
                "Pour simplement l'empêcher de démarrer, utilisez plutôt l'interrupteur (l'entrée est conservée et réactivable).",
                confirmText: "Supprimer");

            if (confirmed)
            {
                await ViewModel.DeleteEntryCommand.ExecuteAsync(entry);
            }
        };
        menu.Items.Add(delete);
        menu.ShowAt(button);
    }
}
