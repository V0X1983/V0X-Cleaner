using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;

namespace V0XCleaner.App.WinUI.ViewModels;

public partial class SystemRestoreViewModel : ObservableObject
{
    private readonly ISystemRestoreManager _manager;
    private readonly ILogger<SystemRestoreViewModel> _logger;

    public ObservableCollection<RestorePointViewModel> RestorePoints { get; } = [];

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Cliquez sur \"Actualiser\" pour lister les points de restauration.";

    [ObservableProperty]
    public partial string NewRestorePointDescription { get; set; } = "Point de restauration V0X Cleaner";

    public SystemRestoreViewModel(ISystemRestoreManager manager, ILogger<SystemRestoreViewModel> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        StatusMessage = "Recherche des points de restauration...";

        try
        {
            var points = await Task.Run(() => _manager.GetRestorePointsAsync());
            RestorePoints.Clear();
            foreach (var point in points)
            {
                RestorePoints.Add(new RestorePointViewModel(point));
            }

            StatusMessage = points.Count > 0
                ? $"{points.Count} point(s) de restauration."
                : "Aucun point de restauration trouvé (la protection du système est peut-être désactivée).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du chargement des points de restauration.");
            StatusMessage = "Erreur lors du chargement des points de restauration.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task CreateAsync()
    {
        IsBusy = true;
        StatusMessage = "Création du point de restauration en cours...";

        try
        {
            var description = string.IsNullOrWhiteSpace(NewRestorePointDescription)
                ? "Point de restauration V0X Cleaner"
                : NewRestorePointDescription;

            var result = await Task.Run(() => _manager.CreateRestorePointAsync(description));
            StatusMessage = result.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création du point de restauration.");
            StatusMessage = "Erreur lors de la création du point de restauration.";
        }
        finally
        {
            IsBusy = false;
        }

        await RefreshAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(RestorePointViewModel? pointVm)
    {
        if (pointVm is null)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var result = await Task.Run(() => _manager.DeleteRestorePointAsync(pointVm.Info.SequenceNumber));
            StatusMessage = result.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la suppression du point de restauration {Sequence}", pointVm.Info.SequenceNumber);
            StatusMessage = "Erreur lors de la suppression du point de restauration.";
        }
        finally
        {
            IsBusy = false;
        }

        await RefreshAsync();
    }

    private bool CanRun() => !IsBusy;

    partial void OnIsBusyChanged(bool value)
    {
        RefreshCommand.NotifyCanExecuteChanged();
        CreateCommand.NotifyCanExecuteChanged();
    }
}
