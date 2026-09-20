using System.ComponentModel;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using V0XCleaner.App.Helpers;
using V0XCleaner.Core.Abstractions;

namespace V0XCleaner.App.ViewModels;

public partial class DiskAnalyzerViewModel : ObservableObject
{
    private readonly IDiskAnalyzer _analyzer;
    private readonly IPathGuard _pathGuard;
    private readonly ILogger<DiskAnalyzerViewModel> _logger;

    public ObservableCollection<string> Drives { get; } = [];

    public ObservableCollection<FolderSizeNodeViewModel> Nodes { get; } = [];

    [ObservableProperty]
    private string? _selectedDrive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoUp))]
    private string _currentPath = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Choisissez un lecteur pour commencer.";

    public bool CanGoUp => !string.IsNullOrEmpty(CurrentPath) &&
                            !string.Equals(Path.GetPathRoot(CurrentPath), CurrentPath, StringComparison.OrdinalIgnoreCase);

    public DiskAnalyzerViewModel(IDiskAnalyzer analyzer, IPathGuard pathGuard, ILogger<DiskAnalyzerViewModel> logger)
    {
        _analyzer = analyzer;
        _pathGuard = pathGuard;
        _logger = logger;

        // DriveInfo.IsReady peut bloquer plusieurs secondes sur un lecteur optique ou une carte
        // mémoire sans média : cette énumération ne doit jamais se faire de façon synchrone dans
        // le constructeur (appelé sur le thread UI lors de la résolution DI/navigation).
        _ = LoadDrivesAsync();
    }

    private async Task LoadDrivesAsync()
    {
        var drives = await Task.Run(() => _analyzer.GetAvailableDrives());
        Drives.Clear();
        foreach (var drive in drives)
        {
            Drives.Add(drive);
        }

        SelectedDrive = Drives.FirstOrDefault();
    }

    partial void OnSelectedDriveChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _ = NavigateToAsync(value);
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private Task RefreshAsync() =>
        NavigateToAsync(string.IsNullOrEmpty(CurrentPath) ? SelectedDrive ?? string.Empty : CurrentPath);

    [RelayCommand]
    private Task OpenNodeAsync(FolderSizeNodeViewModel? nodeVm) =>
        nodeVm is { IsFile: false } ? NavigateToAsync(nodeVm.Node.FullPath) : Task.CompletedTask;

    [RelayCommand]
    private Task GoUpAsync()
    {
        if (string.IsNullOrEmpty(CurrentPath))
        {
            return Task.CompletedTask;
        }

        var parent = Directory.GetParent(CurrentPath)?.FullName;
        return parent is not null ? NavigateToAsync(parent) : Task.CompletedTask;
    }

    [RelayCommand]
    private async Task DeleteNodeAsync(FolderSizeNodeViewModel? nodeVm)
    {
        if (nodeVm is null)
        {
            return;
        }

        if (!_pathGuard.IsSafeToDelete(nodeVm.Node.FullPath, out var reason))
        {
            StatusMessage = $"Suppression refusée : {reason}";
            return;
        }

        IsBusy = true;
        try
        {
            await Task.Run(() =>
            {
                if (nodeVm.IsFile)
                {
                    File.Delete(nodeVm.Node.FullPath);
                }
                else
                {
                    Directory.Delete(nodeVm.Node.FullPath, recursive: true);
                }
            });

            await NavigateToAsync(CurrentPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusMessage = $"Échec de la suppression : {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private static void OpenInExplorer(FolderSizeNodeViewModel? nodeVm)
    {
        if (nodeVm is null)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{nodeVm.Node.FullPath}\"") { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            // ouverture de l'explorateur best-effort : pas bloquant si ça échoue
        }
    }

    private async Task NavigateToAsync(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        IsBusy = true;
        StatusMessage = "Analyse en cours...";

        try
        {
            var result = await Task.Run(() => _analyzer.AnalyzeAsync(path));
            CurrentPath = path;

            var maxSize = result.Count > 0 ? result.Max(n => n.SizeBytes) : 0;
            Nodes.Clear();
            foreach (var node in result)
            {
                var fraction = maxSize > 0 ? (double)node.SizeBytes / maxSize : 0;
                Nodes.Add(new FolderSizeNodeViewModel(node, fraction));
            }

            var total = result.Sum(n => n.SizeBytes);
            StatusMessage = $"{result.Count} élément(s) — {ByteFormatter.Format(total)} au total.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur pendant l'analyse de {Path}", path);
            StatusMessage = "Erreur pendant l'analyse de ce dossier.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanRun() => !IsBusy;

    partial void OnIsBusyChanged(bool value) => RefreshCommand.NotifyCanExecuteChanged();
}
