using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using V0XCleaner.App.WinUI.Helpers;
using V0XCleaner.App.WinUI.ViewModels;
using Windows.System;

namespace V0XCleaner.App.WinUI.Pages;

public sealed partial class OptionsPage : Microsoft.UI.Xaml.Controls.Page
{
    public OptionsViewModel ViewModel { get; }

    public OptionsPage()
    {
        ViewModel = App.Services.GetRequiredService<OptionsViewModel>();
        InitializeComponent();
        DataContext = ViewModel;
    }

    private async void OpenReleaseButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.LatestReleaseUrl) || !Uri.TryCreate(ViewModel.LatestReleaseUrl, UriKind.Absolute, out var uri))
        {
            return;
        }

        var opened = await Launcher.LaunchUriAsync(uri);
        if (!opened)
        {
            await DialogHelper.NotifyAsync(XamlRoot, "Erreur", $"Impossible d'ouvrir le lien : {ViewModel.LatestReleaseUrl}");
        }
    }
}
