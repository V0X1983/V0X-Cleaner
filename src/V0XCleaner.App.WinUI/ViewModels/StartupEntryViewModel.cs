using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.App.WinUI.ViewModels;

public partial class StartupEntryViewModel : ObservableObject
{
    private readonly IStartupManager _manager;
    private bool _suppressApply;

    public StartupEntry Entry { get; }

    public string Name => Entry.Name;
    public string Command => Entry.Command;
    public string Location => Entry.Location;

    public string SourceLabel => Entry.Source switch
    {
        StartupEntrySource.RegistryRun => "Registre",
        StartupEntrySource.StartupFolder => "Dossier Démarrage",
        StartupEntrySource.ScheduledTask => "Tâche planifiée",
        _ => Entry.Source.ToString()
    };

    /// <summary>Chemin du fichier exécutable extrait de la commande de démarrage (sans arguments).</summary>
    public string FilePath { get; }

    /// <summary>Éditeur lu dans les propriétés du fichier ; vide tant que non chargé ou inconnu.</summary>
    [ObservableProperty]
    public partial string Publisher { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsDeleted { get; set; }

    public StartupEntryViewModel(StartupEntry entry, IStartupManager manager)
    {
        Entry = entry;
        _manager = manager;
        FilePath = V0XCleaner.Services.CommandLineHelper.ExtractFilePath(entry.Command);

        _suppressApply = true;
        IsEnabled = entry.IsEnabled;
        _suppressApply = false;
    }

    /// <summary>Charge l'éditeur depuis les métadonnées du fichier (lecture disque, à appeler hors du thread UI).</summary>
    public void LoadPublisher()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                Publisher = FileVersionInfo.GetVersionInfo(FilePath).CompanyName?.Trim() ?? string.Empty;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // Fichier illisible : l'éditeur reste vide.
        }
    }

    partial void OnIsEnabledChanged(bool value)
    {
        if (_suppressApply)
        {
            return;
        }

        _ = ApplyEnabledChangeAsync(value);
    }

    private async Task ApplyEnabledChangeAsync(bool value)
    {
        IsBusy = true;
        StatusMessage = null;

        try
        {
            var ok = await _manager.SetEnabledAsync(Entry, value);
            if (!ok)
            {
                StatusMessage = "Échec (droits administrateur peut-être requis).";

                _suppressApply = true;
                IsEnabled = !value;
                _suppressApply = false;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> DeleteAsync()
    {
        IsBusy = true;
        StatusMessage = null;

        try
        {
            var ok = await _manager.DeleteAsync(Entry);
            if (ok)
            {
                IsDeleted = true;
            }
            else
            {
                StatusMessage = "Échec de la suppression (droits administrateur peut-être requis).";
            }

            return ok;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
