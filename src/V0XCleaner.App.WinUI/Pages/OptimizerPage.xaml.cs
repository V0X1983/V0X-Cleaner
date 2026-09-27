using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using V0XCleaner.App.WinUI.Helpers;
using V0XCleaner.App.WinUI.ViewModels;

namespace V0XCleaner.App.WinUI.Pages;

public sealed partial class OptimizerPage : Page
{
    public OptimizerViewModel ViewModel { get; }

    public OptimizerPage()
    {
        ViewModel = App.Services.GetRequiredService<OptimizerViewModel>();
        InitializeComponent();
        DataContext = ViewModel;

        Loaded += (_, _) =>
        {
            if (!ViewModel.HasProcesses && ViewModel.RefreshCommand.CanExecute(null))
            {
                ViewModel.RefreshCommand.Execute(null);
            }
        };
    }

    private void SleepButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ProcessItemViewModel item })
        {
            ViewModel.SleepCommand.Execute(item);
        }
    }

    private void ResumeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ProcessItemViewModel item })
        {
            ViewModel.ResumeCommand.Execute(item);
        }
    }

    private void IncludeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ExcludedProcessViewModel item })
        {
            ViewModel.IncludeCommand.Execute(item);
        }
    }

    /// <summary>Menu « ... » d'une ligne : fermer l'application, ou l'exclure de la liste si elle est active.</summary>
    private void MoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ProcessItemViewModel item } button)
        {
            return;
        }

        var menu = new MenuFlyout();

        var close = new MenuFlyoutItem { Text = "Fermer l'application" };
        close.Click += async (_, _) => await ConfirmAndCloseAsync(item);
        menu.Items.Add(close);

        if (!ViewModel.Sleeping.Contains(item))
        {
            var exclude = new MenuFlyoutItem { Text = "Exclure de la mise en veille" };
            exclude.Click += async (_, _) => await ViewModel.ExcludeCommand.ExecuteAsync(item);
            menu.Items.Add(exclude);
        }

        menu.ShowAt(button);
    }

    private async Task ConfirmAndCloseAsync(ProcessItemViewModel item)
    {
        var confirmed = await DialogHelper.ConfirmAsync(
            XamlRoot,
            "Confirmer la fermeture",
            $"Fermer \"{item.Name}\" ?\n\nLes données non enregistrées de ce programme seront perdues s'il ne se ferme pas proprement.",
            confirmText: "Fermer");

        if (confirmed)
        {
            await ViewModel.CloseCommand.ExecuteAsync(item);
        }
    }
}
