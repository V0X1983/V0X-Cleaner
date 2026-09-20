using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using V0XCleaner.App.ViewModels;

namespace V0XCleaner.App.Views;

public partial class StartupManagerView : UserControl
{
    public StartupManagerView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is StartupManagerViewModel { HasEntries: false } viewModel && viewModel.RefreshCommand.CanExecute(null))
            {
                viewModel.RefreshCommand.Execute(null);
            }
        };
    }

    /// <summary>Menu « ... » d'une ligne : suppression de l'entrée, après confirmation.</summary>
    private void MoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: StartupEntryViewModel entry } button || DataContext is not StartupManagerViewModel viewModel)
        {
            return;
        }

        var menu = new ContextMenu { PlacementTarget = button, Placement = PlacementMode.Bottom };
        var delete = new MenuItem { Header = "Supprimer l'entrée" };
        delete.Click += async (_, _) =>
        {
            var result = MessageBox.Show(
                $"Supprimer définitivement « {entry.Name} » du démarrage ?\n\n" +
                "Pour simplement l'empêcher de démarrer, utilisez plutôt l'interrupteur (l'entrée est conservée et réactivable).",
                "Confirmer la suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                await viewModel.DeleteEntryCommand.ExecuteAsync(entry);
            }
        };
        menu.Items.Add(delete);
        menu.IsOpen = true;
    }
}
