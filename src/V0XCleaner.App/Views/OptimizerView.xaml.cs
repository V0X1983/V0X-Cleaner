using System.Windows;
using System.Windows.Controls;
using V0XCleaner.App.ViewModels;

namespace V0XCleaner.App.Views;

public partial class OptimizerView : UserControl
{
    public OptimizerView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is OptimizerViewModel { HasProcesses: false } viewModel && viewModel.RefreshCommand.CanExecute(null))
            {
                viewModel.RefreshCommand.Execute(null);
            }
        };
    }

    /// <summary>Menu « ... » d'une ligne : fermer l'application, ou l'exclure de la liste si elle est active.</summary>
    private void MoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ProcessItemViewModel item } button || DataContext is not OptimizerViewModel viewModel)
        {
            return;
        }

        var menu = new ContextMenu { PlacementTarget = button, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom };

        var close = new MenuItem { Header = "Fermer l'application" };
        close.Click += async (_, _) => await ConfirmAndCloseAsync(viewModel, item);
        menu.Items.Add(close);

        if (!viewModel.Sleeping.Contains(item))
        {
            var exclude = new MenuItem { Header = "Exclure de la mise en veille" };
            exclude.Click += async (_, _) => await viewModel.ExcludeCommand.ExecuteAsync(item);
            menu.Items.Add(exclude);
        }

        menu.IsOpen = true;
    }

    private static async Task ConfirmAndCloseAsync(OptimizerViewModel viewModel, ProcessItemViewModel item)
    {
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
