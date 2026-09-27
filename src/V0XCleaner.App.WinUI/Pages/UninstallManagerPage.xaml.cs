using System.IO;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using V0XCleaner.App.WinUI.Helpers;
using V0XCleaner.App.WinUI.ViewModels;

namespace V0XCleaner.App.WinUI.Pages;

public sealed partial class UninstallManagerPage : Page
{
    public UninstallManagerViewModel ViewModel { get; }

    public UninstallManagerPage()
    {
        ViewModel = App.Services.GetRequiredService<UninstallManagerViewModel>();
        InitializeComponent();
        DataContext = ViewModel;
    }

    private async void ProgramRow_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: InstalledProgramViewModel program } && program.Icon is null)
        {
            await program.LoadIconAsync();
        }
    }

    private async void UninstallButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: InstalledProgramViewModel programVm })
        {
            return;
        }

        var confirmed = await DialogHelper.ConfirmAsync(
            XamlRoot,
            "Confirmer la désinstallation",
            $"Désinstaller « {programVm.DisplayName} » ?\n\nCeci lance le désinstalleur officiel du programme.",
            confirmText: "Désinstaller");

        if (confirmed)
        {
            await ViewModel.UninstallCommand.ExecuteAsync(programVm);
        }
    }

    private async void ForceRemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: InstalledProgramViewModel programVm })
        {
            return;
        }

        var confirmed = await DialogHelper.ConfirmAsync(
            XamlRoot,
            "Confirmer la suppression forcée",
            $"Ceci va supprimer directement le dossier d'installation et l'entrée de registre de « {programVm.DisplayName} » " +
            "SANS lancer son désinstalleur.\n\n" +
            "À utiliser seulement si la désinstallation normale a échoué ou si le programme n'apparaît plus que comme un résidu. " +
            "Une sauvegarde .reg sera créée automatiquement avant la modification du registre.\n\nContinuer ?",
            confirmText: "Forcer la suppression");

        if (confirmed)
        {
            await ViewModel.ForceRemoveCommand.ExecuteAsync(programVm);
        }
    }

    private async void MoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: InstalledProgramViewModel programVm })
        {
            return;
        }

        var destination = await PickerHelper.PickFolderAsync();
        if (destination is null)
        {
            return;
        }

        var confirmed = await DialogHelper.ConfirmAsync(
            XamlRoot,
            "Confirmer le déplacement",
            $"Déplacer « {programVm.DisplayName} » vers :\n{destination}\n\n" +
            "Fermez le programme avant de continuer. Une jonction sera laissée à l'ancien emplacement " +
            "et une sauvegarde .reg sera créée automatiquement.\n\nContinuer ?",
            confirmText: "Déplacer");

        if (confirmed)
        {
            await ViewModel.MoveProgramAsync(programVm, destination);
        }
    }

    private async void ExportCsvButton_Click(object sender, RoutedEventArgs e)
    {
        var path = await PickerHelper.PickSaveFileAsync(
            $"v0xcleaner-programmes-{DateTime.Now:yyyyMMdd}.csv",
            "Fichier CSV",
            ".csv");

        if (path is null)
        {
            return;
        }

        try
        {
            File.WriteAllText(path, ViewModel.BuildCsvExport(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }
        catch (IOException ex)
        {
            await DialogHelper.NotifyAsync(XamlRoot, "Erreur", $"Échec de l'export : {ex.Message}");
        }
    }
}
