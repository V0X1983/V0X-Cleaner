using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using V0XCleaner.App.Helpers;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.App.ViewModels;

/// <summary>Une ligne cochable des étapes « confidentialité » et « espace » : regroupe plusieurs tâches du catalogue de nettoyage.</summary>
public sealed partial class HealthCategoryViewModel(string title, string description, string glyph, IReadOnlyList<CleaningTask> tasks) : ObservableObject
{
    public string Title { get; } = title;

    public string Description { get; } = description;

    public string Glyph { get; } = glyph;

    public IReadOnlyList<CleaningTask> Tasks { get; } = tasks;

    /// <summary>Éléments trouvés par l'analyse, par tâche ; c'est ce qui sera nettoyé si la ligne est cochée.</summary>
    public Dictionary<CleaningTask, IReadOnlyList<CleanupItem>> Found { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SizeText))]
    private long _sizeBytes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CountText))]
    private int _itemCount;

    [ObservableProperty]
    private bool _isSelected = true;

    public string SizeText => ByteFormatter.Format(SizeBytes);

    public string CountText => ItemCount.ToString("N0");
}

/// <summary>Une ligne de l'étape « problèmes » : un type de problème détecté et son nombre.</summary>
public sealed class HealthProblemViewModel(string title, string description, string glyph, int count)
{
    public string Title { get; } = title;

    public string Description { get; } = description;

    public string Glyph { get; } = glyph;

    public int Count { get; } = count;
}

/// <summary>
/// Bilan de santé en trois étapes : confidentialité (traces des navigateurs), espace (fichiers
/// temporaires, caches, corbeille) puis problèmes à résoudre (démarrage, applications et pilotes
/// obsolètes, registre). Seules les deux premières étapes peuvent nettoyer, après cochage explicite.
/// </summary>
public partial class HealthCheckViewModel : ObservableObject
{
    private const string PrivacyDescription = "Protégez votre confidentialité en effaçant les traces de vos activités en ligne.";
    private const string SpaceDescription = "Aidez à optimiser votre PC. Récupérez de l'espace disque pour créer de la place pour les fichiers importants.";
    private const string ProblemsDescription = "Résolvez ces problèmes pour réduire les risques de sécurité et accélérer votre PC.";

    private readonly ICleaningCatalog _cleaningCatalog;
    private readonly IRegistryIssueCatalog _registryIssueCatalog;
    private readonly IStartupManager _startupManager;
    private readonly ISoftwareUpdater _softwareUpdater;
    private readonly IDriverCatalog _driverCatalog;
    private readonly IMemoryOptimizer _memoryOptimizer;
    private readonly ILogger<HealthCheckViewModel> _logger;

    private readonly List<HealthCategoryViewModel> _privacy = [];
    private readonly List<HealthCategoryViewModel> _space = [];

    public ScanProgress Loading { get; } = new();

    /// <summary>Lignes de l'étape courante (confidentialité ou espace).</summary>
    public ObservableCollection<HealthCategoryViewModel> Categories { get; } = [];

    public ObservableCollection<HealthProblemViewModel> Problems { get; } = [];

    /// <summary>0 = accueil, 1 = confidentialité, 2 = espace, 3 = problèmes.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIntro))]
    [NotifyPropertyChangedFor(nameof(IsStarted))]
    [NotifyPropertyChangedFor(nameof(IsCleaningStep))]
    [NotifyPropertyChangedFor(nameof(IsProblemsStep))]
    [NotifyPropertyChangedFor(nameof(Headline))]
    [NotifyPropertyChangedFor(nameof(HeadlineDescription))]
    [NotifyPropertyChangedFor(nameof(Step1Done))]
    [NotifyPropertyChangedFor(nameof(Step2Done))]
    [NotifyPropertyChangedFor(nameof(Step1Current))]
    [NotifyPropertyChangedFor(nameof(Step2Current))]
    private int _currentStep;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public bool IsIntro => CurrentStep == 0;

    public bool IsStarted => CurrentStep > 0;

    public bool IsCleaningStep => CurrentStep is 1 or 2;

    public bool IsProblemsStep => CurrentStep == 3;

    public bool Step1Current => CurrentStep == 1;

    public bool Step2Current => CurrentStep == 2;

    public bool Step1Done => CurrentStep > 1;

    public bool Step2Done => CurrentStep > 2;

    public int SelectedItemCount => Categories.Where(c => c.IsSelected).Sum(c => c.ItemCount);

    public long SelectedBytes => Categories.Where(c => c.IsSelected).Sum(c => c.SizeBytes);

    public string SelectionText => $"{ByteFormatter.Format(SelectedBytes)} ({SelectedItemCount:N0} fichiers) sélectionnés";

    public bool CanCleanSelection => !IsBusy && SelectedItemCount > 0;

    /// <summary>Titre de l'étape ; le nombre ou la taille mis en avant est fourni à part par <see cref="HeadlineHighlight"/>.</summary>
    public string HeadlineHighlight => CurrentStep switch
    {
        1 => string.Empty,
        2 => string.Empty,
        3 => $"{TotalProblems} problème{(TotalProblems > 1 ? "s" : string.Empty)}",
        _ => string.Empty
    };

    public string Headline => CurrentStep switch
    {
        1 => $"{Categories.Sum(c => c.ItemCount):N0} enregistrements peuvent être supprimés",
        2 => $"{ByteFormatter.Format(Categories.Sum(c => c.SizeBytes))} peuvent être supprimés",
        3 => $"{(TotalProblems > 1 ? "détectés" : "détecté")} sur votre PC",
        _ => string.Empty
    };

    public string HeadlineDescription => CurrentStep switch
    {
        1 => PrivacyDescription,
        2 => SpaceDescription,
        3 => ProblemsDescription,
        _ => string.Empty
    };

    public int TotalProblems => Problems.Sum(p => p.Count);

    /// <summary>0 = médiocre, 1 = modéré, 2 = bon état ; position du repère sur la jauge de l'étape « problèmes ».</summary>
    public int PcStateIndex => TotalProblems == 0 ? 2 : TotalProblems <= 10 ? 1 : 0;

    public string PcStateText => PcStateIndex switch
    {
        2 => "Bon état",
        1 => "Modéré",
        _ => "Médiocre"
    };

    public string PcStateAdvice => PcStateIndex switch
    {
        2 => "Votre PC est en bonne santé.",
        1 => "Quelques points à améliorer.",
        _ => "Votre PC peut faire mieux. Corrigez ce qui le ralentit."
    };

    public HealthCheckViewModel(
        ICleaningCatalog cleaningCatalog,
        IRegistryIssueCatalog registryIssueCatalog,
        IStartupManager startupManager,
        ISoftwareUpdater softwareUpdater,
        IDriverCatalog driverCatalog,
        IMemoryOptimizer memoryOptimizer,
        ILogger<HealthCheckViewModel> logger)
    {
        _cleaningCatalog = cleaningCatalog;
        _registryIssueCatalog = registryIssueCatalog;
        _startupManager = startupManager;
        _softwareUpdater = softwareUpdater;
        _driverCatalog = driverCatalog;
        _memoryOptimizer = memoryOptimizer;
        _logger = logger;
    }

    /// <summary>Case « tout sélectionner » de l'étape courante (indéterminée si la sélection est partielle).</summary>
    public bool? AllSelected
    {
        get
        {
            if (Categories.Count == 0 || Categories.All(c => c.IsSelected))
            {
                return Categories.Count > 0;
            }

            return Categories.Any(c => c.IsSelected) ? null : false;
        }
        set
        {
            foreach (var category in Categories)
            {
                category.IsSelected = value == true;
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task AnalyzeAsync()
    {
        IsBusy = true;
        StatusMessage = string.Empty;
        var ct = Loading.Begin("Analyse de votre PC en cours...");

        try
        {
            _privacy.Clear();
            _space.Clear();
            Problems.Clear();

            var tasks = _cleaningCatalog.GetTasks().Where(t => t.SelectedByDefault).ToList();
            _privacy.Add(NewCategory("Fichiers temporaires", "Supprimez les fichiers temporaires stockés par les navigateurs web pour protéger votre confidentialité.", "",
                tasks.Where(t => t.Category == CleanupCategory.BrowserCache)));
            _privacy.Add(NewCategory("Cookies", "Supprimez les données de sites web pour ne plus partager vos préférences et vos informations de connexion.", "",
                tasks.Where(t => t.Category == CleanupCategory.BrowserCookies)));
            _privacy.Add(NewCategory("Historique de navigation", "Supprimez les enregistrements des sites web visités, des recherches et des téléchargements.", "",
                tasks.Where(t => t.Category is CleanupCategory.BrowserHistory or CleanupCategory.BrowserDownloadsHistory)));

            _space.Add(NewCategory("Fichiers système temporaires", "Supprimez les fichiers temporaires, journaux, miniatures et vidages mémoire de Windows.", "",
                tasks.Where(t => t.Section == CleaningSection.System
                    && t.Category is CleanupCategory.SystemTemp or CleanupCategory.Logs or CleanupCategory.ThumbnailCache or CleanupCategory.MemoryDumps)));
            _space.Add(NewCategory("Cache d'application", "Libérez l'espace utilisé par les caches des applications et des services de stockage cloud.", "",
                tasks.Where(t => t.Section is CleaningSection.ThirdPartyApplications or CleaningSection.Cloud)));
            _space.Add(NewCategory("Corbeille", "Videz votre corbeille pour libérer un espace de stockage précieux.", "",
                tasks.Where(t => t.Category == CleanupCategory.RecycleBin)));

            var all = _privacy.Concat(_space).SelectMany(c => c.Tasks.Select(t => (Category: c, Task: t))).ToList();
            var done = 0;
            foreach (var (category, task) in all)
            {
                ct.ThrowIfCancellationRequested();
                Loading.Message = $"Analyse : {task.DisplayName}";
                try
                {
                    var scan = await Task.Run(() => task.Scanner.ScanAsync(ct), ct);
                    category.Found[task] = scan.Items;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _logger.LogWarning(ex, "Analyse impossible pour {Task}", task.DisplayName);
                }

                done++;
                Loading.Progress = done * 100.0 / all.Count;
            }

            foreach (var category in _privacy.Concat(_space))
            {
                category.ItemCount = category.Found.Values.Sum(items => items.Count);
                category.SizeBytes = category.Found.Values.Sum(items => items.Sum(i => i.SizeBytes));
            }

            ShowStep(1);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Analyse arrêtée.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur pendant le bilan de santé.");
            StatusMessage = "Erreur pendant le bilan de santé.";
        }
        finally
        {
            Loading.End();
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCleanSelection))]
    private async Task CleanAsync()
    {
        IsBusy = true;
        var ct = Loading.Begin("Nettoyage en cours...", "Arrêter le nettoyage");

        try
        {
            long freed = 0;
            var work = Categories.Where(c => c.IsSelected).SelectMany(c => c.Found).Where(f => f.Value.Count > 0).ToList();
            var done = 0;
            foreach (var (task, items) in work)
            {
                ct.ThrowIfCancellationRequested();
                Loading.Message = $"Nettoyage : {task.DisplayName}";
                var result = await Task.Run(() => task.Cleaner.CleanAsync(items, OperationMode.Execute, ct), ct);
                freed += result.FreedBytes;
                done++;
                Loading.Progress = done * 100.0 / work.Count;
            }

            StatusMessage = $"{ByteFormatter.Format(freed)} libérés.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Nettoyage arrêté.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur pendant le nettoyage du bilan de santé.");
            StatusMessage = "Erreur pendant le nettoyage.";
        }
        finally
        {
            Loading.End();
            IsBusy = false;
        }

        await NextAsync();
    }

    /// <summary>« Ignorer » : passe à l'étape suivante sans rien nettoyer.</summary>
    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task NextAsync()
    {
        switch (CurrentStep)
        {
            case 1:
                ShowStep(2);
                break;
            case 2:
                await LoadProblemsAsync();
                break;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task FreeMemoryAsync()
    {
        IsBusy = true;
        var ct = Loading.Begin("Libération de la mémoire en cours...", "Arrêter");

        try
        {
            var result = await Task.Run(() => _memoryOptimizer.FreeMemoryAsync(ct), ct);
            StatusMessage = $"{result.ProcessesTrimmed} processus optimisé(s), environ {ByteFormatter.Format(result.EstimatedBytesFreed)} libérés.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Libération de la mémoire arrêtée.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur pendant la libération de mémoire.");
            StatusMessage = "Erreur pendant la libération de mémoire.";
        }
        finally
        {
            Loading.End();
            IsBusy = false;
        }
    }

    private async Task LoadProblemsAsync()
    {
        IsBusy = true;
        var ct = Loading.Begin("Recherche des problèmes...", "Arrêter l'analyse");
        var problems = new List<HealthProblemViewModel>();

        try
        {
            Loading.Message = "Analyse des programmes de démarrage...";
            var startup = await Task.Run(() => _startupManager.GetEntriesAsync(ct), ct);
            problems.Add(new HealthProblemViewModel("Applications inutiles au démarrage",
                "Désactivez les applications en arrière-plan au démarrage pour l'accélérer (Outils > Démarrage).",
                "", Math.Max(0, startup.Count(e => e.IsEnabled) - 5)));

            Loading.Message = "Analyse du registre...";
            var registryCount = 0;
            foreach (var task in _registryIssueCatalog.GetTasks())
            {
                ct.ThrowIfCancellationRequested();
                registryCount += (await Task.Run(() => task.Scanner.ScanAsync(ct), ct)).Items.Count;
            }

            problems.Add(new HealthProblemViewModel("Problèmes de registre",
                "Réparez les entrées de registre orphelines (onglet Registre).", "", registryCount));

            Loading.Message = "Recherche des applications obsolètes (winget)...";
            var software = await Task.Run(() => _softwareUpdater.ScanAsync(ct), ct);
            problems.Add(new HealthProblemViewModel("Applications obsolètes",
                "Mettez à jour les applications pour éviter les risques de sécurité (Outils > Mise à jour de logiciels).",
                "", software.Updates.Count));

            Loading.Message = "Recherche des pilotes obsolètes (Windows Update, peut prendre une minute)...";
            var drivers = await Task.Run(() => _driverCatalog.GetAvailableUpdatesAsync(ct), ct);
            problems.Add(new HealthProblemViewModel("Pilotes obsolètes",
                "Mettez à jour les pilotes pour éviter les risques de sécurité (Outils > Mise à jour de pilotes).",
                "", drivers.Count));
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Analyse interrompue : résultats partiels.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur pendant la recherche des problèmes.");
            StatusMessage = "Erreur pendant la recherche des problèmes.";
        }
        finally
        {
            Loading.End();
            IsBusy = false;
        }

        Problems.Clear();
        foreach (var problem in problems)
        {
            Problems.Add(problem);
        }

        OnPropertyChanged(nameof(TotalProblems));
        OnPropertyChanged(nameof(PcStateIndex));
        OnPropertyChanged(nameof(PcStateText));
        OnPropertyChanged(nameof(PcStateAdvice));
        OnPropertyChanged(nameof(HeadlineHighlight));
        ShowStep(3);
    }

    private static HealthCategoryViewModel NewCategory(string title, string description, string glyph, IEnumerable<CleaningTask> tasks)
        => new(title, description, glyph, tasks.ToList());

    private void ShowStep(int step)
    {
        foreach (var category in Categories)
        {
            category.PropertyChanged -= OnCategoryChanged;
        }

        Categories.Clear();
        foreach (var category in step switch { 1 => _privacy, 2 => _space, _ => [] })
        {
            category.PropertyChanged += OnCategoryChanged;
            Categories.Add(category);
        }

        CurrentStep = step;
        RefreshSelection();
    }

    private void OnCategoryChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HealthCategoryViewModel.IsSelected))
        {
            RefreshSelection();
        }
    }

    private void RefreshSelection()
    {
        OnPropertyChanged(nameof(SelectedItemCount));
        OnPropertyChanged(nameof(SelectedBytes));
        OnPropertyChanged(nameof(SelectionText));
        OnPropertyChanged(nameof(CanCleanSelection));
        OnPropertyChanged(nameof(AllSelected));
        OnPropertyChanged(nameof(Headline));
        CleanCommand.NotifyCanExecuteChanged();
    }

    private bool CanRun() => !IsBusy;

    partial void OnIsBusyChanged(bool value)
    {
        AnalyzeCommand.NotifyCanExecuteChanged();
        CleanCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
        FreeMemoryCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanCleanSelection));
    }
}
