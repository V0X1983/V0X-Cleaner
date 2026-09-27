using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using Windows.System;

namespace V0XCleaner.App.WinUI.ViewModels;

public partial class DriverRowViewModel : ObservableObject
{
    public string? UpdateId { get; init; }

    public required string Title { get; init; }

    public required string VersionText { get; init; }

    public required string DateText { get; init; }

    public bool Selectable { get; init; }

    public string? ActionLabel { get; init; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

public partial class DriverGroupViewModel(string name, IReadOnlyList<DriverRowViewModel> rows) : ObservableObject
{
    private bool _updating;

    public string Header => $"{name} ({rows.Count})";

    public IReadOnlyList<DriverRowViewModel> Rows { get; } = rows;

    public bool ShowCheckbox => Rows.Any(r => r.Selectable);

    public bool? IsChecked
    {
        get
        {
            var selectable = Rows.Where(r => r.Selectable).ToList();
            if (selectable.Count == 0 || selectable.All(r => !r.IsSelected))
            {
                return false;
            }

            return selectable.All(r => r.IsSelected) ? true : null;
        }
        set
        {
            _updating = true;
            foreach (var row in Rows.Where(r => r.Selectable))
            {
                row.IsSelected = value == true;
            }

            _updating = false;
            OnPropertyChanged();
        }
    }

    public void Refresh()
    {
        if (!_updating)
        {
            OnPropertyChanged(nameof(IsChecked));
        }
    }
}

public partial class DriversViewModel : ObservableObject
{
    private readonly IDriverCatalog _catalog;
    private readonly ISettingsService _settings;
    private readonly IElevationService _elevation;
    private readonly ILogger<DriversViewModel> _logger;

    private IReadOnlyList<DriverUpdateInfo> _updates = [];
    private IReadOnlyList<DriverInfo> _installed = [];
    private DateTime? _lastScan;

    public ScanProgress Loading { get; } = new();

    public ObservableCollection<DriverGroupViewModel> Groups { get; } = [];

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial int SelectedTab { get; set; }

    [ObservableProperty]
    public partial string ObsoleteTabTitle { get; set; } = "Pilotes obsolètes (0)";

    [ObservableProperty]
    public partial string UpToDateTabTitle { get; set; } = "Pilotes à jour (0)";

    [ObservableProperty]
    public partial string IgnoredTabTitle { get; set; } = "Omis et ignorés (0)";

    [ObservableProperty]
    public partial string Headline { get; set; } = "Cliquez sur « Analyser à nouveau » pour rechercher des pilotes.";

    [ObservableProperty]
    public partial bool HasSelection { get; set; }

    [ObservableProperty]
    public partial string SelectionText { get; set; } = "Aucune sélection";

    [ObservableProperty]
    public partial string LastScanText { get; set; } = "Aucune analyse effectuée";

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    public bool IsObsoleteTab
    {
        get => SelectedTab == 0;
        set { if (value) SelectedTab = 0; }
    }

    public bool IsUpToDateTab
    {
        get => SelectedTab == 1;
        set { if (value) SelectedTab = 1; }
    }

    public bool IsIgnoredTab
    {
        get => SelectedTab == 2;
        set { if (value) SelectedTab = 2; }
    }

    public bool IsElevated => _elevation.IsElevated;

    public DriversViewModel(IDriverCatalog catalog, ISettingsService settings, IElevationService elevation, ILogger<DriversViewModel> logger)
    {
        _catalog = catalog;
        _settings = settings;
        _elevation = elevation;
        _logger = logger;
    }

    partial void OnSelectedTabChanged(int value)
    {
        OnPropertyChanged(nameof(IsObsoleteTab));
        OnPropertyChanged(nameof(IsUpToDateTab));
        OnPropertyChanged(nameof(IsIgnoredTab));
        Rebuild();
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ScanAsync()
    {
        IsBusy = true;
        StatusMessage = null;
        var ct = Loading.Begin("Recherche de pilotes obsolètes...");

        try
        {
            var installedTask = _catalog.GetDriversAsync(ct);
            var updatesTask = _catalog.GetAvailableUpdatesAsync(ct);
            _installed = await installedTask;
            _updates = await updatesTask;
            _lastScan = DateTime.Now;
            StatusMessage = null;
            Rebuild();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Analyse arrêtée.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'analyse des pilotes.");
            StatusMessage = "Erreur lors de l'analyse des pilotes.";
        }
        finally
        {
            Loading.End();
            IsBusy = false;
        }
    }

    public async Task InstallSelectedAsync()
    {
        var ids = SelectedIds();
        if (ids.Count == 0 || IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = $"Installation de {ids.Count} pilote(s) via Windows Update...";

        try
        {
            var error = await _catalog.InstallUpdatesAsync(ids);
            StatusMessage = error ?? "Installation terminée. Un redémarrage peut être nécessaire.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'installation des pilotes.");
            StatusMessage = "Erreur lors de l'installation des pilotes.";
        }
        finally
        {
            IsBusy = false;
        }

        await ScanAsync();
    }

    public IReadOnlyList<string> SelectedIds() =>
        Groups.SelectMany(g => g.Rows).Where(r => r.IsSelected && r.UpdateId is not null).Select(r => r.UpdateId!).ToList();

    [RelayCommand]
    private async Task RowActionAsync(DriverRowViewModel? row)
    {
        if (row?.UpdateId is null)
        {
            return;
        }

        var ignored = _settings.Current.IgnoredDriverUpdateIds;
        if (!ignored.Remove(row.UpdateId))
        {
            ignored.Add(row.UpdateId);
        }

        await _settings.SaveAsync();
        Rebuild();
    }

    [RelayCommand]
    private async Task OpenWindowsUpdateAsync()
    {
        try
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:windowsupdate"));
        }
        catch (Exception ex) when (ex is FileNotFoundException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Impossible d'ouvrir Windows Update.");
        }
    }

    private void Rebuild()
    {
        foreach (var row in Groups.SelectMany(g => g.Rows))
        {
            row.PropertyChanged -= OnRowChanged;
        }

        var ignoredIds = _settings.Current.IgnoredDriverUpdateIds;
        var obsolete = _updates.Where(u => !ignoredIds.Contains(u.Id)).ToList();
        var ignored = _updates.Where(u => ignoredIds.Contains(u.Id)).ToList();

        ObsoleteTabTitle = $"Pilotes obsolètes ({obsolete.Count})";
        UpToDateTabTitle = $"Pilotes à jour ({_installed.Count})";
        IgnoredTabTitle = $"Omis et ignorés ({ignored.Count})";

        Groups.Clear();
        switch (SelectedTab)
        {
            case 0:
                Headline = obsolete.Count == 0
                    ? (_lastScan is null ? "Cliquez sur « Analyser à nouveau » pour rechercher des pilotes." : "Tous vos pilotes sont à jour.")
                    : $"{obsolete.Count} pilote(s) à mettre à jour";
                AddUpdateGroups(obsolete, "Ignorer");
                break;
            case 1:
                Headline = $"{_installed.Count} pilote(s) installé(s)";
                foreach (var group in _installed.GroupBy(d => string.IsNullOrEmpty(d.DeviceClass) ? "Autres" : d.DeviceClass).OrderBy(g => g.Key))
                {
                    var rows = group.Select(d => new DriverRowViewModel
                    {
                        Title = d.DeviceName,
                        VersionText = d.Version,
                        DateText = d.Date is { } date ? date.ToString("dd/MM/yyyy") : "—"
                    }).ToList();
                    Groups.Add(new DriverGroupViewModel(group.Key, rows));
                }

                break;
            default:
                Headline = $"{ignored.Count} pilote(s) ignoré(s)";
                AddUpdateGroups(ignored, "Réactiver");
                break;
        }

        foreach (var row in Groups.SelectMany(g => g.Rows))
        {
            row.PropertyChanged += OnRowChanged;
        }

        LastScanText = _lastScan is { } scan
            ? $"Dernière analyse : {scan:HH:mm}"
            : "Aucune analyse effectuée";
        UpdateSelectionText();
    }

    private void AddUpdateGroups(IEnumerable<DriverUpdateInfo> updates, string actionLabel)
    {
        foreach (var group in updates.GroupBy(u => string.IsNullOrEmpty(u.DeviceClass) ? "Autres" : u.DeviceClass).OrderBy(g => g.Key))
        {
            var rows = group.Select(u => new DriverRowViewModel
            {
                UpdateId = u.Id,
                Title = u.Title,
                VersionText = u.Version,
                DateText = u.Published is { } date ? date.ToString("dd/MM/yyyy") : "—",
                Selectable = actionLabel == "Ignorer",
                ActionLabel = actionLabel
            }).ToList();
            Groups.Add(new DriverGroupViewModel(group.Key, rows));
        }
    }

    private void OnRowChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DriverRowViewModel.IsSelected))
        {
            foreach (var group in Groups)
            {
                group.Refresh();
            }

            UpdateSelectionText();
        }
    }

    private void UpdateSelectionText()
    {
        var count = SelectedIds().Count;
        SelectionText = count == 0 ? "Aucune sélection" : $"{count} sélectionné(s)";
        HasSelection = count > 0 && !IsBusy;
    }

    private bool CanRun() => !IsBusy;

    partial void OnIsBusyChanged(bool value)
    {
        ScanCommand.NotifyCanExecuteChanged();
        HasSelection = !value && SelectedIds().Count > 0;
    }
}
