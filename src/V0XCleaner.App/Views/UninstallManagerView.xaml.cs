using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using V0XCleaner.App.ViewModels;

namespace V0XCleaner.App.Views;

public partial class UninstallManagerView : UserControl
{
    public UninstallManagerView()
    {
        InitializeComponent();
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
        if (sender is not Button { Tag: InstalledProgramViewModel programVm } || DataContext is not UninstallManagerViewModel viewModel)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Désinstaller \"{programVm.DisplayName}\" ?\n\nCeci lance le désinstalleur officiel du programme.",
            "Confirmer la désinstallation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        await viewModel.UninstallCommand.ExecuteAsync(programVm);
    }

    private async void ForceRemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: InstalledProgramViewModel programVm } || DataContext is not UninstallManagerViewModel viewModel)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Ceci va supprimer directement le dossier d'installation et l'entrée de registre de \"{programVm.DisplayName}\" " +
            "SANS lancer son désinstalleur.\n\n" +
            "À utiliser seulement si la désinstallation normale a échoué ou si le programme n'apparaît plus que comme un résidu. " +
            "Une sauvegarde .reg sera créée automatiquement avant la modification du registre.\n\nContinuer ?",
            "Confirmer la suppression forcée",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        await viewModel.ForceRemoveCommand.ExecuteAsync(programVm);
    }

    private async void MoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: InstalledProgramViewModel programVm } || DataContext is not UninstallManagerViewModel viewModel)
        {
            return;
        }

        var dialog = new OpenFolderDialog
        {
            Title = $"Choisir le dossier de destination pour \"{programVm.DisplayName}\""
        };

        // S'ouvre sur le dossier qui contient l'application, pour voir où elle se trouve actuellement.
        try
        {
            var current = Path.GetFullPath(programVm.Program.InstallLocation!.Trim().Trim('"')).TrimEnd('\\');
            var parent = Path.GetDirectoryName(current);
            if (parent is not null && Directory.Exists(parent))
            {
                dialog.InitialDirectory = parent;
            }
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
        }

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Déplacer \"{programVm.DisplayName}\" vers :\n{dialog.FolderName}\n\n" +
            "Fermez le programme avant de continuer. Une jonction sera laissée à l'ancien emplacement " +
            "et une sauvegarde .reg sera créée automatiquement.\n\nContinuer ?",
            "Confirmer le déplacement",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        await viewModel.MoveProgramAsync(programVm, dialog.FolderName);
    }

    private void ExportCsvButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not UninstallManagerViewModel viewModel)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            FileName = $"v0xcleaner-programmes-{DateTime.Now:yyyyMMdd}.csv",
            Filter = "Fichier CSV (*.csv)|*.csv",
            DefaultExt = ".csv"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            File.WriteAllText(dialog.FileName, viewModel.BuildCsvExport(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Échec de l'export : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
