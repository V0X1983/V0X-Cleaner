using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.App.ViewModels;

public partial class StartupEntryViewModel : ObservableObject
{
    private readonly IStartupManager _manager;

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
    private string _publisher = string.Empty;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isDeleted;

    public StartupEntryViewModel(StartupEntry entry, IStartupManager manager)
    {
        Entry = entry;
        _manager = manager;
        FilePath = V0XCleaner.Services.CommandLineHelper.ExtractFilePath(entry.Command);
        _isEnabled = entry.IsEnabled; // affectation directe : ne déclenche pas OnIsEnabledChanged
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

                // Écriture directe du champ généré (au lieu de la propriété IsEnabled) : repasser par le
                // setter re-déclencherait OnIsEnabledChanged et un nouvel appel à SetEnabledAsync, avec un
                // risque de va-et-vient infini si celui-ci échoue aussi. On ne fait ici que corriger l'affichage.
#pragma warning disable MVVMTK0034
                _isEnabled = !value;
#pragma warning restore MVVMTK0034
                OnPropertyChanged(nameof(IsEnabled));
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
