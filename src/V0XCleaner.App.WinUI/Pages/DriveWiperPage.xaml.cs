using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using V0XCleaner.App.WinUI.Helpers;
using V0XCleaner.App.WinUI.ViewModels;

namespace V0XCleaner.App.WinUI.Pages;

public sealed partial class DriveWiperPage : Page
{
    public DriveWiperViewModel ViewModel { get; }
    private bool _driveComboRefreshed;

    public DriveWiperPage()
    {
        ViewModel = App.Services.GetRequiredService<DriveWiperViewModel>();
        InitializeComponent();
        DataContext = ViewModel;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    /// <summary>
    /// Piège découvert en pratique : quand `SelectedDrive` est affecté par le ViewModel (chargement
    /// asynchrone des lecteurs) avant que le ComboBox n'ait réalisé ses conteneurs au moins une fois,
    /// le texte affiché en fermé (SelectionBoxItem) ne se met pas à jour — la sélection est pourtant
    /// bien prise en compte (CanStartNow réagit correctement), seul l'affichage reste vide. Ouvrir puis
    /// refermer le menu déroulant force WinUI à resynchroniser l'affichage ; fait une seule fois, à la
    /// première sélection programmatique, pour ne jamais perturber une sélection faite par l'utilisateur.
    /// </summary>
    private async void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_driveComboRefreshed || e.PropertyName != nameof(DriveWiperViewModel.SelectedDrive))
        {
            return;
        }

        _driveComboRefreshed = true;
        DriveComboBox.IsDropDownOpen = true;
        await Task.Delay(100);
        DriveComboBox.IsDropDownOpen = false;
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(ViewModel.SelectedDrive))
        {
            return;
        }

        var isSystemDrive = string.Equals(
            ViewModel.SelectedDrive.TrimEnd('\\'),
            Environment.GetEnvironmentVariable("SystemDrive"),
            StringComparison.OrdinalIgnoreCase);

        var systemDriveWarning = isSystemDrive
            ? "\n\n⚠ Il s'agit du lecteur système (celui où Windows est installé). Le remplissage temporaire de son " +
              "espace libre peut ralentir ou perturber le système pendant l'opération."
            : string.Empty;

        var confirmed = await DialogHelper.ConfirmAsync(
            XamlRoot,
            "Confirmer l'effacement de l'espace libre",
            $"Effacer l'espace libre du lecteur {ViewModel.SelectedDrive} avec la méthode « {ViewModel.SelectedMethod.Label} » ?\n\n" +
            "Le lecteur sera temporairement rempli à sa capacité maximale pendant l'opération. " +
            "Cela peut prendre longtemps selon la taille du disque. N'éteignez pas l'ordinateur pendant ce temps." +
            systemDriveWarning +
            "\n\nContinuer ?",
            confirmText: "Effacer");

        if (confirmed)
        {
            await ViewModel.StartCommand.ExecuteAsync(null);
        }
    }
}
