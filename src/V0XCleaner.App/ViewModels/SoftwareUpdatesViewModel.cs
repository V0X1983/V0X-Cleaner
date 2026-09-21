using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using V0XCleaner.App.Infrastructure;
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

    /// <summary>Vrai quand la dernière tentative de mise à jour a échoué (le statut s'affiche alors en rouge).</summary>
    [ObservableProperty]
    private bool _isError;

    /// <summary>Vrai pendant le téléchargement et l'installation ; affiche la barre de progression de la ligne.</summary>
    [ObservableProperty]
    private bool _isWorking;

    /// <summary>Avancement du téléchargement (0-100).</summary>
    [ObservableProperty]
    private double _progressValue;

    /// <summary>Vrai quand l'avancement est inconnu (installation, ou taille du téléchargement non annoncée).</summary>
    [ObservableProperty]
    private bool _isIndeterminate = true;
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
        using var guard = ShutdownGuard.Begin("Mise à jour de logiciels en cours");
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
        using var guard = ShutdownGuard.Begin("Mise à jour de logiciels en cours");
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
        item.IsError = false;
        item.ProgressValue = 0;
        item.IsIndeterminate = true;
        item.IsWorking = true;

        var phase = SoftwareUpdatePhase.Downloading;
        var finished = false;
        Stopwatch? installWatch = null;

        // winget ne donne aucun pourcentage pour l'installation : on affiche une barre animée et le temps écoulé.
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) =>
        {
            if (!finished && installWatch is not null)
            {
                var elapsed = installWatch.Elapsed;
                item.Status = $"Installation... {(int)elapsed.TotalMinutes}:{elapsed.Seconds:00}";
            }
        };

        var progress = new Progress<SoftwareUpdateProgress>(p =>
        {
            // Les rapports arrivent en différé : on ignore ceux d'une phase passée ou d'une mise à jour terminée.
            if (finished || p.Phase < phase)
            {
                return;
            }

            phase = p.Phase;
            if (p.Phase == SoftwareUpdatePhase.Installing)
            {
                if (installWatch is null)
                {
                    installWatch = Stopwatch.StartNew();
                    item.IsIndeterminate = true;
                    item.Status = "Installation... 0:00";
                    timer.Start();
                }

                return;
            }

            if (p.Percent is { } percent)
            {
                item.IsIndeterminate = false;
                item.ProgressValue = percent;
                item.Status = $"Téléchargement {percent:0} %";
            }
            else
            {
                item.IsIndeterminate = true;
                item.Status = "Téléchargement...";
            }
        });

        try
        {
            var result = await Task.Run(() => _updater.UpdateAsync(item.Update.Id, progress));
            finished = true;
            item.Status = result.Success ? "Mis à jour" : result.Message ?? "Échec de la mise à jour.";
            item.IsUpdated = result.Success;
            item.IsError = !result.Success;
        }
        catch (Exception ex)
        {
            finished = true;
            _logger.LogError(ex, "Erreur de mise à jour de {Id}", item.Update.Id);
            item.Status = "Erreur";
            item.IsError = true;
        }
        finally
        {
            timer.Stop();
            item.IsWorking = false;
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
