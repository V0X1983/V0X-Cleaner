using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using V0XCleaner.Core.Abstractions;

namespace V0XCleaner.App.ViewModels;

public partial class QuarantineViewModel : ObservableObject
{
    private readonly IQuarantineService _quarantine;
    private readonly ISettingsService _settings;

    private const int MaxDisplayed = 300;

    public ScanProgress Loading { get; } = new();

    public ObservableCollection<QuarantineEntryViewModel> Entries { get; } = [];

    [ObservableProperty]
    private string _statusMessage = "Cliquez sur \"Actualiser\" pour lister les fichiers en quarantaine.";

    public QuarantineViewModel(IQuarantineService quarantine, ISettingsService settings)
    {
        _quarantine = quarantine;
        _settings = settings;
        Refresh();
    }

    [RelayCommand]
    private void Refresh()
    {
        var all = _quarantine.GetEntries();
        var shown = all.Take(MaxDisplayed).Select(e => new QuarantineEntryViewModel(e)).ToList();

        Entries.Clear();
        foreach (var vm in shown)
        {
            Entries.Add(vm);
        }

        StatusMessage = all.Count == 0
            ? "La quarantaine est vide."
            : all.Count > MaxDisplayed
                ? $"{all.Count} fichier(s) en quarantaine — les {MaxDisplayed} plus récents sont affichés (purge automatique après {_settings.Current.QuarantineRetentionDays} jour(s))."
                : $"{all.Count} fichier(s) en quarantaine (purge automatique après {_settings.Current.QuarantineRetentionDays} jour(s)).";
    }

    [RelayCommand]
    private void Restore(QuarantineEntryViewModel? vm)
    {
        if (vm is null)
        {
            return;
        }

        var ok = _quarantine.Restore(vm.Entry.Id, out var error);
        Refresh();
        StatusMessage = ok ? "Fichier restauré." : $"Échec de la restauration : {error}";
    }

    [RelayCommand]
    private void Purge(QuarantineEntryViewModel? vm)
    {
        if (vm is null)
        {
            return;
        }

        _quarantine.PurgeEntry(vm.Entry.Id);
        Refresh();
    }

    [RelayCommand]
    private async Task PurgeAllAsync()
    {
        var ct = Loading.Begin("Suppression définitive en cours...", "Arrêter la suppression");
        try
        {
            var progress = new Progress<double>(percent => Loading.Progress = percent);
            var deleted = await Task.Run(() => _quarantine.PurgeExpired(TimeSpan.Zero, progress, ct), ct);
            StatusMessage = $"{deleted} élément(s) supprimé(s) définitivement.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Suppression arrêtée.";
        }
        finally
        {
            Loading.End();
        }

        var summary = StatusMessage;
        Refresh();
        StatusMessage = summary;
    }
}
