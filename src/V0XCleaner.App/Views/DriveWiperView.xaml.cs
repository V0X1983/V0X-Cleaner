using System.Windows;
using System.Windows.Controls;
using V0XCleaner.App.ViewModels;

namespace V0XCleaner.App.Views;

public partial class DriveWiperView : UserControl
{
    public DriveWiperView()
    {
        InitializeComponent();
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not DriveWiperViewModel viewModel || string.IsNullOrEmpty(viewModel.SelectedDrive))
        {
            return;
        }

        var isSystemDrive = string.Equals(
            viewModel.SelectedDrive.TrimEnd('\\'),
            Environment.GetEnvironmentVariable("SystemDrive"),
            StringComparison.OrdinalIgnoreCase);

        var systemDriveWarning = isSystemDrive
            ? "\n\n⚠ Il s'agit du lecteur système (celui où Windows est installé). Le remplissage temporaire de son " +
              "espace libre peut ralentir ou perturber le système pendant l'opération."
            : string.Empty;

        var result = MessageBox.Show(
            $"Effacer l'espace libre du lecteur {viewModel.SelectedDrive} avec la méthode \"{viewModel.SelectedMethod.Label}\" ?\n\n" +
            "Le lecteur sera temporairement rempli à sa capacité maximale pendant l'opération. " +
            "Cela peut prendre longtemps selon la taille du disque. N'éteignez pas l'ordinateur pendant ce temps." +
            systemDriveWarning +
            "\n\nContinuer ?",
            "Confirmer l'effacement de l'espace libre",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        await viewModel.StartCommand.ExecuteAsync(null);
    }
}
