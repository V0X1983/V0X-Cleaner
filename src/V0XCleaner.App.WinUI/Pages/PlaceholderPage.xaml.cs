using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using V0XCleaner.App.WinUI.ViewModels;

namespace V0XCleaner.App.WinUI.Pages;

/// <summary>
/// Page générique affichée pour un module pas encore porté vers WinUI 3.
/// Remplacée module par module au fil des phases de migration (voir PROMPT.md).
/// </summary>
public sealed partial class PlaceholderPage : Page
{
    public PlaceholderPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is PlaceholderPageViewModel viewModel)
        {
            TitleText.Text = viewModel.Title;
            DescriptionText.Text = viewModel.Description;
        }
    }
}
