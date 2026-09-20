using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using V0XCleaner.App.Helpers;
using V0XCleaner.Core.Abstractions;

namespace V0XCleaner.App.ViewModels;

public partial class DuplicateFinderViewModel : ObservableObject
{
    private readonly IDuplicateFileFinder _finder;
    private readonly IPathGuard _pathGuard;
    private readonly IQuarantineService _quarantine;
    private readonly ISettingsService _settings;
    private readonly ILogger<DuplicateFinderViewModel> _logger;

    public ObservableCollection<DuplicateGroupViewModel> Groups { get; } = [];

    [ObservableProperty]
    private string _folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Choisissez un dossier puis cliquez sur \"Analyser\".";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedTotalWasted))]
    private long _totalWastedBytes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedSelected))]
    private long _selectedBytes;

    public string FormattedTotalWasted => ByteFormatter.Format(TotalWastedBytes);

    public string FormattedSelected => ByteFormatter.Format(SelectedBytes);

    [ObservableProperty]
    private bool _canDeleteNow;

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
        Groups.Clear();
        TotalWastedBytes = 0;
        SelectedBytes = 0;

        var progress = new Progress<string>(message => StatusMessage = message);

        try
        {
            var groups = await Task.Run(() => _finder.FindDuplicatesAsync(FolderPath, progress));

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur pendant la recherche de doublons dans {Path}", FolderPath);
            StatusMessage = "Erreur pendant la recherche de doublons.";
        }
        finally
        {
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
