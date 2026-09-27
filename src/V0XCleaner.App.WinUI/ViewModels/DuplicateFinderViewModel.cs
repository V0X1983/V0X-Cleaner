using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using V0XCleaner.App.WinUI.Helpers;
using V0XCleaner.Core.Abstractions;

namespace V0XCleaner.App.WinUI.ViewModels;

public partial class DuplicateFinderViewModel : ObservableObject
{
    private readonly IDuplicateFileFinder _finder;
    private readonly IPathGuard _pathGuard;
    private readonly IQuarantineService _quarantine;
    private readonly ISettingsService _settings;
    private readonly ILogger<DuplicateFinderViewModel> _logger;

    public ObservableCollection<DuplicateGroupViewModel> Groups { get; } = [];

    [ObservableProperty]
    public partial string FolderPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Choisissez un dossier puis cliquez sur \"Analyser\".";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedTotalWasted))]
    public partial long TotalWastedBytes { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedSelected))]
    [NotifyPropertyChangedFor(nameof(SelectedLabel))]
    public partial long SelectedBytes { get; set; }

    public string FormattedTotalWasted => ByteFormatter.Format(TotalWastedBytes);

    public string FormattedSelected => ByteFormatter.Format(SelectedBytes);

    public string SelectedLabel => $"Sélectionné : {FormattedSelected}";

    [ObservableProperty]
    public partial bool CanDeleteNow { get; set; }

    public DuplicateFinderViewModel(IDuplicateFileFinder finder, IPathGuard pathGuard, IQuarantineService quarantine, ISettingsService settings, ILogger<DuplicateFinderViewModel> logger)
    {
        _finder = finder;
        _pathGuard = pathGuard;
        _quarantine = quarantine;
        _settings = settings;
        _logger = logger;
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ScanAsync()
    {
        if (!Directory.Exists(FolderPath))
        {
            StatusMessage = "Ce dossier n'existe pas.";
            return;
        }

        IsBusy = true;
        var ct = Loading.Begin("Recherche de doublons en cours...");
        Groups.Clear();
        TotalWastedBytes = 0;
        SelectedBytes = 0;

        var progress = new Progress<string>(message =>
        {
            StatusMessage = message;
            Loading.Message = message;
        });

        try
        {
            var groups = await Task.Run(() => _finder.FindDuplicatesAsync(FolderPath, progress, ct), ct);

            foreach (var group in groups)
            {
                var entries = group.FilePaths
                    .Select((path, index) => new DuplicateFileEntryViewModel(path) { IsSelected = index > 0 })
                    .ToList();

                foreach (var entry in entries)
                {
                    entry.PropertyChanged += (_, e) =>
                    {
                        if (e.PropertyName == nameof(DuplicateFileEntryViewModel.IsSelected))
                        {
                            RecomputeSelected();
                        }
                    };
                }

                Groups.Add(new DuplicateGroupViewModel(group, entries));
            }

            TotalWastedBytes = groups.Sum(g => g.WastedBytes);
            RecomputeSelected();

            StatusMessage = groups.Count > 0
                ? $"{groups.Count} groupe(s) de doublons — {FormattedTotalWasted} récupérables."
                : "Aucun doublon trouvé.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Recherche arrêtée.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur pendant la recherche de doublons dans {Path}", FolderPath);
            StatusMessage = "Erreur pendant la recherche de doublons.";
        }
        finally
        {
            Loading.End();
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteSelectedAsync()
    {
        IsBusy = true;

        try
        {
            var toDelete = Groups.SelectMany(g => g.Entries).Where(e => e.IsSelected).Select(e => e.FilePath).ToList();
            var deleted = 0;
            var failed = 0;

            await Task.Run(() =>
            {
                foreach (var filePath in toDelete)
                {
                    if (!_pathGuard.IsSafeToDelete(filePath, out _))
                    {
                        failed++;
                        continue;
                    }

                    try
                    {
                        if (!_settings.Current.QuarantineEnabled || _quarantine.Quarantine(filePath) is null)
                        {
                            File.Delete(filePath);
                        }

                        deleted++;
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        failed++;
                    }
                }
            });

            _quarantine.Flush();

            StatusMessage = failed == 0
                ? $"{deleted} fichier(s) supprimé(s)."
                : $"{deleted} fichier(s) supprimé(s), {failed} échec(s).";
        }
        finally
        {
            IsBusy = false;
        }

        await ScanAsync();
    }

    public ScanProgress Loading { get; } = new();

    private bool CanRun() => !IsBusy;

    private bool CanDelete() => !IsBusy && Groups.Any(g => g.Entries.Any(e => e.IsSelected));

    partial void OnIsBusyChanged(bool value)
    {
        ScanCommand.NotifyCanExecuteChanged();
        DeleteSelectedCommand.NotifyCanExecuteChanged();
        CanDeleteNow = CanDelete();
    }

    private void RecomputeSelected()
    {
        long total = 0;
        foreach (var group in Groups)
        {
            total += group.Entries.Count(e => e.IsSelected) * group.Group.SizeBytes;
        }

        SelectedBytes = total;
        DeleteSelectedCommand.NotifyCanExecuteChanged();
        CanDeleteNow = CanDelete();
    }
}
