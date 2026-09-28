using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;

namespace V0XCleaner.App.WinUI.ViewModels;

public partial class UninstallManagerViewModel : ObservableObject
{
    private readonly IInstalledProgramsCatalog _catalog;
    private readonly ILogger<UninstallManagerViewModel> _logger;
    private List<InstalledProgramViewModel> _allPrograms = [];

    public ObservableCollection<InstalledProgramViewModel> DisplayedPrograms { get; } = [];

    public IReadOnlyList<ProgramSortOption> SortOptions { get; } =
    [
        new(ProgramSortMode.NameAscending, "Nom (A→Z)"),
        new(ProgramSortMode.NameDescending, "Nom (Z→A)"),
        new(ProgramSortMode.SizeDescending, "Taille (plus grand d'abord)"),
        new(ProgramSortMode.SizeAscending, "Taille (plus petit d'abord)"),
        new(ProgramSortMode.PublisherAscending, "Éditeur"),
        new(ProgramSortMode.InstallDateDescending, "Date d'installation (récent d'abord)")
    ];

    /// <summary>Glyphe affiché dans l'en-tête « Programme » quand le tri actuel porte sur le nom (cliquable, bascule A→Z / Z→A).</summary>
    public string NameSortGlyph => SelectedSortOption.Mode switch
    {
        ProgramSortMode.NameAscending => "▲",
        ProgramSortMode.NameDescending => "▼",
        _ => string.Empty
    };

    /// <summary>Glyphe affiché dans l'en-tête « Taille » quand le tri actuel porte sur la taille (cliquable, bascule les deux sens).</summary>
    public string SizeSortGlyph => SelectedSortOption.Mode switch
    {
        ProgramSortMode.SizeDescending => "▼",
        ProgramSortMode.SizeAscending => "▲",
        _ => string.Empty
    };

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Cliquez sur \"Actualiser\" pour lister les programmes installés.";

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ProgramSortOption SelectedSortOption { get; set; }

    public UninstallManagerViewModel(IInstalledProgramsCatalog catalog, ILogger<UninstallManagerViewModel> logger)
    {
        _catalog = catalog;
        _logger = logger;
        SelectedSortOption = SortOptions[0];
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        StatusMessage = "Recherche des programmes installés...";

        try
        {
            var programs = await Task.Run(() => _catalog.GetProgramsAsync());
            _allPrograms = programs.Select(p => new InstalledProgramViewModel(p)).ToList();
            ApplyFilterAndSort();
            StatusMessage = $"{_allPrograms.Count} programme(s) trouvé(s).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du chargement des programmes installés.");
            StatusMessage = "Erreur lors du chargement des programmes installés.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task UninstallAsync(InstalledProgramViewModel? programVm)
    {
        if (programVm is null)
        {
            return;
        }

        programVm.IsBusy = true;
        programVm.StatusMessage = null;

        try
        {
            var result = await Task.Run(() => _catalog.UninstallAsync(programVm.Program, preferSilent: false));
            programVm.StatusMessage = result.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la désinstallation de {Name}", programVm.DisplayName);
            programVm.StatusMessage = "Erreur inattendue pendant la désinstallation.";
        }
        finally
        {
            programVm.IsBusy = false;
        }

        await RefreshAsync();
    }

    [RelayCommand]
    private async Task ForceRemoveAsync(InstalledProgramViewModel? programVm)
    {
        if (programVm is null)
        {
            return;
        }

        programVm.IsBusy = true;
        programVm.StatusMessage = null;

        try
        {
            var result = await Task.Run(() => _catalog.ForceRemoveResidueAsync(programVm.Program));
            programVm.StatusMessage = result.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la suppression forcée de {Name}", programVm.DisplayName);
            programVm.StatusMessage = "Erreur inattendue pendant la suppression forcée.";
        }
        finally
        {
            programVm.IsBusy = false;
        }

        await RefreshAsync();
    }

    public async Task MoveProgramAsync(InstalledProgramViewModel programVm, string destinationParentFolder)
    {
        programVm.IsBusy = true;
        programVm.StatusMessage = "Déplacement en cours...";

        try
        {
            var result = await Task.Run(() => _catalog.MoveAsync(programVm.Program, destinationParentFolder));
            programVm.StatusMessage = result.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du déplacement de {Name}", programVm.DisplayName);
            programVm.StatusMessage = "Erreur inattendue pendant le déplacement.";
        }
        finally
        {
            programVm.IsBusy = false;
        }
    }

    public string BuildCsvExport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Nom;Version;Éditeur;Taille;Date d'installation;Type");

        foreach (var p in DisplayedPrograms)
        {
            sb.AppendLine(string.Join(';',
                CsvEscape(p.DisplayName),
                CsvEscape(p.DisplayVersion),
                CsvEscape(p.Publisher),
                CsvEscape(p.FormattedSize),
                CsvEscape(p.FormattedInstallDate),
                CsvEscape(p.KindLabel)));
        }

        return sb.ToString();
    }

    private static string CsvEscape(string value) =>
        value.Contains(';') || value.Contains('"') || value.Contains('\n')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;

    private bool CanRun() => !IsBusy;

    partial void OnIsBusyChanged(bool value) => RefreshCommand.NotifyCanExecuteChanged();

    partial void OnSearchTextChanged(string value) => ApplyFilterAndSort();

    partial void OnSelectedSortOptionChanged(ProgramSortOption value)
    {
        OnPropertyChanged(nameof(NameSortGlyph));
        OnPropertyChanged(nameof(SizeSortGlyph));
        ApplyFilterAndSort();
    }

    /// <summary>Clic sur l'en-tête « Programme » : bascule A→Z / Z→A (A→Z par défaut si le tri actuel ne porte pas sur le nom).</summary>
    [RelayCommand]
    private void SortByName() =>
        SelectedSortOption = SortOptions.First(o => o.Mode ==
            (SelectedSortOption.Mode == ProgramSortMode.NameAscending ? ProgramSortMode.NameDescending : ProgramSortMode.NameAscending));

    /// <summary>Clic sur l'en-tête « Taille » : bascule les deux sens (plus grand d'abord par défaut si le tri actuel ne porte pas sur la taille).</summary>
    [RelayCommand]
    private void SortBySize() =>
        SelectedSortOption = SortOptions.First(o => o.Mode ==
            (SelectedSortOption.Mode == ProgramSortMode.SizeDescending ? ProgramSortMode.SizeAscending : ProgramSortMode.SizeDescending));

    private void ApplyFilterAndSort()
    {
        IEnumerable<InstalledProgramViewModel> query = _allPrograms;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(p =>
                p.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                p.Publisher.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        query = SelectedSortOption.Mode switch
        {
            ProgramSortMode.NameAscending => query.OrderBy(p => p.DisplayName, StringComparer.CurrentCultureIgnoreCase),
            ProgramSortMode.NameDescending => query.OrderByDescending(p => p.DisplayName, StringComparer.CurrentCultureIgnoreCase),
            ProgramSortMode.SizeDescending => query.OrderByDescending(p => p.Program.EstimatedSizeBytes ?? 0),
            ProgramSortMode.SizeAscending => query.OrderBy(p => p.Program.EstimatedSizeBytes ?? 0),
            ProgramSortMode.PublisherAscending => query.OrderBy(p => p.Publisher, StringComparer.CurrentCultureIgnoreCase),
            ProgramSortMode.InstallDateDescending => query.OrderByDescending(p => p.Program.InstallDate ?? DateTime.MinValue),
            _ => query
        };

        DisplayedPrograms.Clear();
        foreach (var p in query)
        {
            DisplayedPrograms.Add(p);
        }
    }
}
