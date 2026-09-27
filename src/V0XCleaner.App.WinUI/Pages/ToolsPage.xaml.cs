using Microsoft.UI.Xaml.Controls;
using V0XCleaner.App.WinUI.ViewModels;

namespace V0XCleaner.App.WinUI.Pages;

public sealed partial class ToolsPage : Page
{
    public ToolsPageViewModel ViewModel { get; } = new();

    public ToolsPage()
    {
        InitializeComponent();
        DataContext = ViewModel;
        Loaded += (_, _) => ToolList.SelectedItem = ViewModel.ToolItems[0];
    }

    private void ToolList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0 || e.AddedItems[0] is not NavigationItem item)
        {
            return;
        }

        ViewModel.SelectedToolItem = item;

        switch (item.Key)
        {
            case "system-restore":
                ToolFrame.Navigate(typeof(SystemRestorePage));
                return;
            case "quarantine":
                ToolFrame.Navigate(typeof(QuarantinePage));
                return;
            case "startup":
                ToolFrame.Navigate(typeof(StartupManagerPage));
                return;
            case "drivers":
                ToolFrame.Navigate(typeof(DriversPage));
                return;
            case "software-updates":
                ToolFrame.Navigate(typeof(SoftwareUpdatesPage));
                return;
            case "optimizer":
                ToolFrame.Navigate(typeof(OptimizerPage));
                return;
        }

        ToolFrame.Navigate(typeof(PlaceholderPage), new PlaceholderPageViewModel
        {
            Title = item.Title,
            Description = $"Le module « {item.Title} » n'a pas encore été porté vers WinUI 3."
        });
    }
}
