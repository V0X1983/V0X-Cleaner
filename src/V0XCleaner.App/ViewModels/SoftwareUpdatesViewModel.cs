using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.App.ViewModels;

public partial class SoftwareUpdateItemViewModel(SoftwareUpdate update) : ObservableObject
{
    public SoftwareUpdate Update { get; } = update;

    public string Name => Update.Name;

    public string VersionText => $"{Update.CurrentVersion} → {Update.AvailableVersion}";

    [ObservableProperty]
    private string _status = string.Empty;
}

public partial class SoftwareUpdatesViewModel : ObservableObject
{
    private readonly ISoftwareUpdater _updater;
    private readonly ILogger<SoftwareUpdatesViewModel> _logger;

    public ScanProgress Loading { get; } = new();

    public ObservableCollection<SoftwareUpdateItemViewModel> Updates { get; } = [];

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Cliquez sur \"Analyser\" pour rechercher les mises à jour de vos logiciels.";

    public SoftwareUpdatesViewModel(ISoftwareUpdater updater, ILogger<SoftwareUpdatesViewModel> logger)
    {
        _updater = updater;
        _logger = logger;
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ScanAsync()
    {
        IsBusy = true;
        StatusMessage = "Recherche des mises à jour (winget)...";
        var ct = Loading.Begin("Recherche de mises à jour de logiciels...");

        try
        {
            var scan = await Task.Run(() => _updater.ScanAsync(ct), ct);
            Updates.Clear();
            foreach (var update in scan.Updates)
            {
                Updates.Add(new SoftwareUpdateItemViewModel(update));
            }

            StatusMessage = scan.Error ?? (Updates.Count > 0
                ? $"{Updates.Count} mise(s) à jour disponible(s)."
                : "Tous vos logiciels sont à jour.");
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Recherche arrêtée.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la recherche de mises à jour.");
            StatusMessage = "Erreur lors de la recherche de mises à jour.";
        }
        finally
        {
            Loading.End();
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task UpdateOneAsync(SoftwareUpdateItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await UpdateItemAsync(item);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task UpdateAllAsync()
    {
        IsBusy = true;
        try
        {
            foreach (var item in Updates.ToList())
            {
                await UpdateItemAsync(item);
            }

            StatusMessage = "Mises à jour terminées. Relancez l'analyse pour vérifier.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UpdateItemAsync(SoftwareUpdateItemViewModel item)
    {
        item.Status = "Mise à jour en cours...";
        try
        {
            var ok = await Task.Run(() => _updater.UpdateAsync(item.Update.Id));
            item.Status = ok ? "Mis à jour" : "Échec (droits administrateur ou application ouverte ?)";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur de mise à jour de {Id}", item.Update.Id);
            item.Status = "Erreur";
        }
    }

    private bool CanRun() => !IsBusy;

    partial void OnIsBusyChanged(bool value)
    {
        ScanCommand.NotifyCanExecuteChanged();
        UpdateOneCommand.NotifyCanExecuteChanged();
        UpdateAllCommand.NotifyCanExecuteChanged();
    }
}
