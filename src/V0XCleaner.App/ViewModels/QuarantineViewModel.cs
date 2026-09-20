using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using V0XCleaner.Core.Abstractions;

namespace V0XCleaner.App.ViewModels;

public partial class QuarantineViewModel : ObservableObject
{
    private readonly IQuarantineService _quarantine;
    private readonly ISettingsService _settings;

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
        Entries.Clear();
        foreach (var entry in _quarantine.GetEntries())
        {
            Entries.Add(new QuarantineEntryViewModel(entry));
        }

        StatusMessage = Entries.Count > 0
            ? $"{Entries.Count} fichier(s) en quarantaine (purge automatique après {_settings.Current.QuarantineRetentionDays} jour(s))."
            : "La quarantaine est vide.";
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
    private void PurgeAll()
    {
        _quarantine.PurgeExpired(TimeSpan.Zero);
        Refresh();
    }
}
