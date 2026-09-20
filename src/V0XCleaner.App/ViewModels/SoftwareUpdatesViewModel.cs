using System.Collections.ObjectModel;
using System.Diagnostics;
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

    /// <summary>Version installée ; devient la nouvelle version une fois la mise à jour réussie.</summary>
    public string CurrentVersion => IsUpdated ? Update.AvailableVersion : Update.CurrentVersion;

    public string AvailableVersion => Update.AvailableVersion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentVersion))]
    [NotifyPropertyChangedFor(nameof(CanUpdate))]
    private bool _isUpdated;

    [ObservableProperty]
    private bool _isSelected = true;

    public bool CanUpdate => !IsUpdated;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private bool _isLookingUpWebsite;
}

public partial class SoftwareUpdatesViewModel : ObservableObject
{
    private readonly ISoftwareUpdater _updater;
    private readonly ILogger<SoftwareUpdatesViewModel> _logger;

    public ScanProgress Loading { get; } = new();

    public ObservableCollection<SoftwareUpdateItemViewModel> Updates { get; } = [];

    [ObservableProperty]
    private bool _isBusy;

    public int SelectedCount => Updates.Count(u => u.IsSelected && !u.IsUpdated);

    public bool HasUpdates => Updates.Count > 0;

    public string HeadlineCount => Updates.Count > 1 ? $"{Updates.Count} mises à jour" : $"{Updates.Count} mise à jour";

    public string SelectedText => SelectedCount > 1 ? $"{SelectedCount} sélectionnés" : $"{SelectedCount} sélectionné";

    /// <summary>Case « tout sélectionner » : vraie si tout est coché, indéterminée si la sélection est partielle.</summary>
    public bool? AllSelected
    {
        get
        {
            var pending = Updates.Where(u => !u.IsUpdated).ToList();
            if (pending.Count == 0 || pending.All(u => u.IsSelected))
            {
                return pending.Count == 0 ? false : true;
            }

            return pending.Any(u => u.IsSelected) ? null : false;
        }
        set
        {
            foreach (var item in Updates.Where(u => !u.IsUpdated))
            {
                item.IsSelected = value == true;
            }
        }
    }

    private void RefreshSelection()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(SelectedText));
        OnPropertyChanged(nameof(AllSelected));
        UpdateSelectedCommand.NotifyCanExecuteChanged();
    }

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
                var item = new SoftwareUpdateItemViewModel(update);
                item.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName is nameof(SoftwareUpdateItemViewModel.IsSelected) or nameof(SoftwareUpdateItemViewModel.IsUpdated))
                    {
                        RefreshSelection();
                    }
                };
                Updates.Add(item);
            }

            OnPropertyChanged(nameof(HasUpdates));
            OnPropertyChanged(nameof(HeadlineCount));
            RefreshSelection();

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

    [RelayCommand(CanExecute = nameof(CanUpdateSelected))]
    private async Task UpdateSelectedAsync()
    {
        IsBusy = true;
        try
        {
            foreach (var item in Updates.Where(u => u.IsSelected && !u.IsUpdated).ToList())
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
        item.Status = "Téléchargement...";
        var phase = SoftwareUpdatePhase.Downloading;
        var progress = new Progress<SoftwareUpdateProgress>(p =>
        {
            // Ne jamais revenir en arrière : l'installation suit le téléchargement.
            if (p.Phase < phase)
            {
                return;
            }

            phase = p.Phase;
            item.Status = p.Phase == SoftwareUpdatePhase.Installing
                ? "Installation..."
                : p.Percent is { } pct ? $"Téléchargement {pct:0} %" : "Téléchargement...";
        });

        try
        {
            var ok = await Task.Run(() => _updater.UpdateAsync(item.Update.Id, progress));
            item.Status = ok ? "Mis à jour" : "Échec (droits administrateur ou application ouverte ?)";
            item.IsUpdated = ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur de mise à jour de {Id}", item.Update.Id);
            item.Status = "Erreur";
        }
    }

    [RelayCommand]
    private async Task OpenWebsiteAsync(SoftwareUpdateItemViewModel? item)
    {
        if (item is null || item.IsLookingUpWebsite)
        {
            return;
        }

        item.IsLookingUpWebsite = true;
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var uri = await Task.Run(() => _updater.GetWebsiteAsync(item.Update.Id, cts.Token));
            if (uri is null)
            {
                StatusMessage = $"Aucun site web trouvé pour {item.Name}.";
                return;
            }

            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossible d'ouvrir le site de {Id}", item.Update.Id);
            StatusMessage = $"Impossible d'ouvrir le site de {item.Name}.";
        }
        finally
        {
            item.IsLookingUpWebsite = false;
        }
    }

    private bool CanRun() => !IsBusy;

    private bool CanUpdateSelected() => !IsBusy && SelectedCount > 0;

    partial void OnIsBusyChanged(bool value)
    {
        ScanCommand.NotifyCanExecuteChanged();
        UpdateOneCommand.NotifyCanExecuteChanged();
        UpdateSelectedCommand.NotifyCanExecuteChanged();
    }
}
