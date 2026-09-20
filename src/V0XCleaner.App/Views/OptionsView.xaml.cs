using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using V0XCleaner.App.ViewModels;

namespace V0XCleaner.App.Views;

public partial class OptionsView : UserControl
{
    public OptionsView()
    {
        InitializeComponent();
    }

    private void OpenReleaseButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not OptionsViewModel viewModel || string.IsNullOrWhiteSpace(viewModel.LatestReleaseUrl))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(viewModel.LatestReleaseUrl) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            MessageBox.Show($"Impossible d'ouvrir le lien : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
