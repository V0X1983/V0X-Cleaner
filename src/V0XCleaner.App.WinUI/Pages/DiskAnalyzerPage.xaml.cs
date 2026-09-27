using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using V0XCleaner.App.WinUI.Helpers;
using V0XCleaner.App.WinUI.ViewModels;

namespace V0XCleaner.App.WinUI.Pages;

public sealed partial class DiskAnalyzerPage : Page
{
    public DiskAnalyzerViewModel ViewModel { get; }
    private bool _driveComboRefreshed;

    public DiskAnalyzerPage()
    {
        ViewModel = App.Services.GetRequiredService<DiskAnalyzerViewModel>();
        InitializeComponent();
        DataContext = ViewModel;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    /// <summary>Voir DriveWiperPage.ViewModel_PropertyChanged : même piège de rafraîchissement de ComboBox.</summary>
    private async void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_driveComboRefreshed || e.PropertyName != nameof(DiskAnalyzerViewModel.SelectedDrive))
        {
            return;
        }

        _driveComboRefreshed = true;
        DriveComboBox.IsDropDownOpen = true;
        await Task.Delay(100);
        DriveComboBox.IsDropDownOpen = false;
    }

    private async void Node_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: FolderSizeNodeViewModel nodeVm })
        {
            await ViewModel.OpenNodeCommand.ExecuteAsync(nodeVm);
        }
    }

    private void OpenInExplorerButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: FolderSizeNodeViewModel nodeVm })
        {
            ViewModel.OpenInExplorerCommand.Execute(nodeVm);
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: FolderSizeNodeViewModel nodeVm })
        {
            return;
        }

        var kind = nodeVm.IsFile ? "le fichier" : "le dossier (et tout son contenu)";
        var confirmed = await DialogHelper.ConfirmAsync(
            XamlRoot,
            "Confirmer la suppression",
            $"Supprimer définitivement {kind} « {nodeVm.Name} » ({nodeVm.FormattedSize}) ?",
            confirmText: "Supprimer");

        if (confirmed)
        {
            await ViewModel.DeleteNodeCommand.ExecuteAsync(nodeVm);
        }
    }
}
