using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using V0XCleaner.App.WinUI.ViewModels;

namespace V0XCleaner.App.WinUI.Pages;

public sealed partial class HelpPage : Page
{
    public HelpViewModel ViewModel { get; }

    public HelpPage()
    {
        ViewModel = App.Services.GetRequiredService<HelpViewModel>();
        InitializeComponent();
        DataContext = ViewModel;
    }
}
