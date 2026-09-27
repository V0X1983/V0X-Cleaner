using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using V0XCleaner.App.WinUI.ViewModels;

namespace V0XCleaner.App.WinUI.Pages;

public sealed partial class SoftwareUpdatesPage : Page
{
    public SoftwareUpdatesViewModel ViewModel { get; }

    public SoftwareUpdatesPage()
    {
        ViewModel = App.Services.GetRequiredService<SoftwareUpdatesViewModel>();
        InitializeComponent();
        DataContext = ViewModel;
    }

    private void OpenWebsiteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SoftwareUpdateItemViewModel item })
        {
            ViewModel.OpenWebsiteCommand.Execute(item);
        }
    }

    private void UpdateOneButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SoftwareUpdateItemViewModel item })
        {
            ViewModel.UpdateOneCommand.Execute(item);
        }
    }
}
