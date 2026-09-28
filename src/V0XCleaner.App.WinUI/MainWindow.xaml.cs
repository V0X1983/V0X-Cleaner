using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using V0XCleaner.App.WinUI.Pages;
using V0XCleaner.App.WinUI.ViewModels;
using V0XCleaner.Core.Abstractions;
using Windows.System;

namespace V0XCleaner.App.WinUI;

public sealed partial class MainWindow : Window
{
    public MainWindowViewModel ViewModel { get; }

    public MainWindow(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        RootGrid.DataContext = ViewModel;

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.SetIcon("Assets/AppIcon.ico");
        AppWindow.Closing += OnAppWindowClosing;

        NavFrame.Navigate(typeof(HealthCheckPage));
    }

    /// <summary>
    /// Phase 5 (icône de zone de notification) : si la surveillance en temps réel est activée, la
    /// fermeture de la fenêtre (bouton X, Alt+F4) masque l'app au lieu de la terminer — seul le
    /// bouton "Quitter" du menu de la zone de notification termine réellement le processus. Sinon,
    /// comportement WPF conservé à l'identique (fermeture normale).
    /// </summary>
    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        args.Cancel = true;

        if (App.Services.GetRequiredService<ISettingsService>().Current.RealTimeMonitoringEnabled)
        {
            sender.Hide();
        }
        else
        {
            App.Shutdown();
        }
    }

    /// <summary>Applique le thème choisi dans les paramètres (résolu via les ThemeDictionaries de Resources/Theme.xaml).</summary>
    public void ApplyTheme(string themeName)
    {
        RootGrid.RequestedTheme = themeName == "Light" ? ElementTheme.Light : ElementTheme.Dark;
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        NavFrame.GoBack();
    }

    private async void UpdateAvailableLabel_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.UpdateAvailableUrl) || !Uri.TryCreate(ViewModel.UpdateAvailableUrl, UriKind.Absolute, out var uri))
        {
            return;
        }

        await Launcher.LaunchUriAsync(uri);
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationItem navItem)
        {
            return;
        }

        switch (navItem.Key)
        {
            case "health-check":
                NavFrame.Navigate(typeof(HealthCheckPage));
                return;
            case "cleaner":
                NavFrame.Navigate(typeof(CleanerPage));
                return;
            case "registry":
                NavFrame.Navigate(typeof(RegistryPage));
                return;
            case "help":
                NavFrame.Navigate(typeof(HelpPage));
                return;
            case "tools":
                NavFrame.Navigate(typeof(ToolsPage));
                return;
            case "options":
                NavFrame.Navigate(typeof(OptionsPage));
                return;
        }

        NavFrame.Navigate(typeof(PlaceholderPage), new PlaceholderPageViewModel
        {
            Title = navItem.Title,
            Description = $"Le module « {navItem.Title} » n'a pas encore été porté vers WinUI 3."
        });
    }
}
