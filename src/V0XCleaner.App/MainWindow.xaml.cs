using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using V0XCleaner.App.ViewModels;

namespace V0XCleaner.App;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void UpdateAvailableLabel_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || string.IsNullOrWhiteSpace(viewModel.UpdateAvailableUrl))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(viewModel.UpdateAvailableUrl) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            MessageBox.Show($"Impossible d'ouvrir le lien : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
