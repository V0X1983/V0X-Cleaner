using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.Cleaners;
using V0XCleaner.Services.Definitions;
using V0XCleaner.Services.Scanners;

namespace V0XCleaner.Services;

/// <summary>
/// Point d'assemblage unique de toutes les tâches de nettoyage disponibles (Étape 1).
/// Construit la liste une seule fois (les scanners eux-mêmes s'exécutent à la demande,
/// cette classe ne fait que déclarer QUOI peut être nettoyé et COMMENT).
/// </summary>
public sealed class CleaningCatalog(
    IPathGuard pathGuard,
    IQuarantineService quarantineService,
    ISettingsService settingsService,
    ILoggerFactory loggerFactory) : ICleaningCatalog
{
    public IReadOnlyList<CleaningTask> GetTasks()
    {
        var tasks = new List<CleaningTask>();
        tasks.AddRange(BuildSystemTasks());
        tasks.AddRange(BuildBrowserTasks());
        tasks.AddRange(BuildThirdPartyTasks());
        return tasks;
    }

    private FileDeletionCleaner NewFileDeletionCleaner() =>
        new(pathGuard, quarantineService, settingsService, loggerFactory.CreateLogger<FileDeletionCleaner>());

    private IEnumerable<CleaningTask> BuildSystemTasks()
    {
        yield return new CleaningTask
        {
            Key = "system-temp",
            DisplayName = "Fichiers temporaires Windows",
            Description = "Fichiers temporaires de l'utilisateur et du système (%TEMP%, Windows\\Temp).",
            Section = CleaningSection.System,
            Category = CleanupCategory.SystemTemp,
            Scanner = new PathPatternScanner("system-temp", CleanupCategory.SystemTemp,
            [
                "%Temp%",
                "%LocalAppData%\\Temp",
                "%SystemRoot%\\Temp"
            ]),
            Cleaner = NewFileDeletionCleaner(),
            SelectedByDefault = true
        };

        yield return new CleaningTask
        {
            Key = "windows-update-cache",
            DisplayName = "Cache Windows Update",
            Description = "Fichiers d'installation téléchargés par Windows Update et déjà appliqués.",
            Section = CleaningSection.System,
            Category = CleanupCategory.SystemTemp,
            Scanner = new PathPatternScanner("windows-update-cache", CleanupCategory.SystemTemp,
            [
                "%SystemRoot%\\SoftwareDistribution\\Download"
            ]),
            Cleaner = NewFileDeletionCleaner(),
            SelectedByDefault = true
        };

        yield return new CleaningTask
        {
            Key = "recycle-bin",
            DisplayName = "Corbeille",
            Description = "Vide la Corbeille pour tous les lecteurs.",
            Section = CleaningSection.System,
            Category = CleanupCategory.RecycleBin,
            Scanner = new RecycleBinScanner(),
            Cleaner = new RecycleBinCleaner(loggerFactory.CreateLogger<RecycleBinCleaner>()),
            SelectedByDefault = true
        };

        yield return new CleaningTask
        {
            Key = "windows-logs-wer",
            DisplayName = "Journaux et rapports d'erreurs Windows",
            Description = "Rapports d'erreurs Windows (WER) déjà envoyés ou en attente.",
            Section = CleaningSection.System,
            Category = CleanupCategory.Logs,
            Scanner = new PathPatternScanner("windows-logs-wer", CleanupCategory.Logs,
            [
                "%ProgramData%\\Microsoft\\Windows\\WER\\ReportArchive",
                "%ProgramData%\\Microsoft\\Windows\\WER\\ReportQueue",
                "%LocalAppData%\\Microsoft\\Windows\\WER\\ReportArchive",
                "%LocalAppData%\\Microsoft\\Windows\\WER\\ReportQueue"
            ]),
            Cleaner = NewFileDeletionCleaner(),
            SelectedByDefault = true
        };

        yield return new CleaningTask
        {
            Key = "thumbnail-icon-cache",
            DisplayName = "Cache des miniatures et icônes",
            Description = "Bases de données de miniatures et d'icônes générées par l'Explorateur Windows.",
            Section = CleaningSection.System,
            Category = CleanupCategory.ThumbnailCache,
            Scanner = new PathPatternScanner("thumbnail-icon-cache", CleanupCategory.ThumbnailCache,
            [
                "%LocalAppData%\\Microsoft\\Windows\\Explorer\\thumbcache_*.db",
                "%LocalAppData%\\Microsoft\\Windows\\Explorer\\iconcache_*.db"
            ]),
            Cleaner = NewFileDeletionCleaner(),
            SelectedByDefault = true
        };

        yield return new CleaningTask
        {
            Key = "memory-dumps",
            DisplayName = "Fichiers de dump mémoire",
            Description = "Rapports de plantage détaillés (.dmp) générés après un crash applicatif ou système.",
            Section = CleaningSection.System,
            Category = CleanupCategory.MemoryDumps,
            Scanner = new PathPatternScanner("memory-dumps", CleanupCategory.MemoryDumps,
            [
                "%LocalAppData%\\CrashDumps\\*.dmp",
                "%SystemRoot%\\Minidump\\*.dmp",
                "%SystemRoot%\\MEMORY.DMP"
            ]),
            Cleaner = NewFileDeletionCleaner(),
            SelectedByDefault = true
        };

        yield return new CleaningTask
        {
            Key = "registry-mru",
            DisplayName = "Historique récent (Exécuter, chemins tapés)",
            Description = "Entrées mémorisées par la boîte \"Exécuter\" et les chemins tapés dans l'Explorateur.",
            Section = CleaningSection.System,
            Category = CleanupCategory.ClipboardAndMru,
            Scanner = new RegistryMruScanner(),
            Cleaner = new RegistryMruCleaner(loggerFactory.CreateLogger<RegistryMruCleaner>()),
            SelectedByDefault = true
        };

        yield return new CleaningTask
        {
            Key = "clipboard",
            DisplayName = "Presse-papiers",
            Description = "Contenu actuellement copié dans le presse-papiers.",
            Section = CleaningSection.System,
            Category = CleanupCategory.ClipboardAndMru,
            Scanner = new ClipboardScanner(),
            Cleaner = new ClipboardCleaner(loggerFactory.CreateLogger<ClipboardCleaner>()),
            SelectedByDefault = false
        };

        yield return new CleaningTask
        {
            Key = "dns-cache",
            DisplayName = "Cache DNS",
            Description = "Résolutions DNS mises en cache par Windows.",
            Section = CleaningSection.System,
            Category = CleanupCategory.DnsCache,
            Scanner = new DnsCacheScanner(),
            Cleaner = new DnsCacheCleaner(loggerFactory.CreateLogger<DnsCacheCleaner>()),
            SelectedByDefault = false
        };
    }

    private IEnumerable<CleaningTask> BuildBrowserTasks()
    {
        foreach (var task in BuildChromiumTasks("chrome", "Google Chrome",
                     "%LocalAppData%\\Google\\Chrome\\User Data"))
        {
            yield return task;
        }

        foreach (var task in BuildChromiumTasks("edge", "Microsoft Edge",
                     "%LocalAppData%\\Microsoft\\Edge\\User Data"))
        {
            yield return task;
        }

        foreach (var task in BuildChromiumTasks("brave", "Brave",
                     "%LocalAppData%\\BraveSoftware\\Brave-Browser\\User Data"))
        {
            yield return task;
        }

        foreach (var task in BuildFirefoxTasks())
        {
            yield return task;
        }
    }

    private IEnumerable<CleaningTask> BuildChromiumTasks(string browserKey, string browserDisplayName, string userDataPattern)
    {
        var userDataPath = Environment.ExpandEnvironmentVariables(userDataPattern);
        if (!Directory.Exists(userDataPath))
        {
            yield break;
        }

        yield return new CleaningTask
        {
            Key = $"{browserKey}-cache",
            DisplayName = $"Cache {browserDisplayName}",
            Description = $"Cache de navigation de {browserDisplayName} (pages, images, scripts).",
            Section = CleaningSection.Browsers,
            Category = CleanupCategory.BrowserCache,
            Scanner = new PathPatternScanner($"{browserKey}-cache", CleanupCategory.BrowserCache,
            [
                $"{userDataPattern}\\*\\Cache",
                $"{userDataPattern}\\*\\Code Cache",
                $"{userDataPattern}\\*\\GPUCache"
            ]),
            Cleaner = NewFileDeletionCleaner(),
            SelectedByDefault = true
        };

        yield return new CleaningTask
        {
            Key = $"{browserKey}-cookies",
            DisplayName = $"Cookies {browserDisplayName}",
            Description = $"Cookies enregistrés par {browserDisplayName} (déconnectera la plupart des sites).",
            Section = CleaningSection.Browsers,
            Category = CleanupCategory.BrowserCookies,
            Scanner = new PathPatternScanner($"{browserKey}-cookies", CleanupCategory.BrowserCookies,
            [
                $"{userDataPattern}\\*\\Network\\Cookies",
                $"{userDataPattern}\\*\\Cookies"
            ]),
            Cleaner = NewFileDeletionCleaner(),
            SelectedByDefault = false
        };

        yield return new CleaningTask
        {
            Key = $"{browserKey}-history",
            DisplayName = $"Historique {browserDisplayName}",
            Description = $"Historique de navigation et de téléchargements de {browserDisplayName}.",
            Section = CleaningSection.Browsers,
            Category = CleanupCategory.BrowserHistory,
            Scanner = new PathPatternScanner($"{browserKey}-history", CleanupCategory.BrowserHistory,
            [
                $"{userDataPattern}\\*\\History",
                $"{userDataPattern}\\*\\History-journal"
            ]),
            Cleaner = NewFileDeletionCleaner(),
            SelectedByDefault = false
        };

        yield return new CleaningTask
        {
            Key = $"{browserKey}-formdata",
            DisplayName = $"Données de formulaires {browserDisplayName}",
            Description = $"Auto-remplissage et données de formulaires enregistrées par {browserDisplayName}.",
            Section = CleaningSection.Browsers,
            Category = CleanupCategory.BrowserFormData,
            Scanner = new PathPatternScanner($"{browserKey}-formdata", CleanupCategory.BrowserFormData,
            [
                $"{userDataPattern}\\*\\Web Data",
                $"{userDataPattern}\\*\\Web Data-journal"
            ]),
            Cleaner = NewFileDeletionCleaner(),
            SelectedByDefault = false
        };

        yield return new CleaningTask
        {
            Key = $"{browserKey}-sessions",
            DisplayName = $"Sessions {browserDisplayName}",
            Description = $"Onglets et fenêtres restaurables de {browserDisplayName} (fermer le navigateur avant nettoyage).",
            Section = CleaningSection.Browsers,
            Category = CleanupCategory.BrowserSessions,
            Scanner = new PathPatternScanner($"{browserKey}-sessions", CleanupCategory.BrowserSessions,
            [
                $"{userDataPattern}\\*\\Sessions",
                $"{userDataPattern}\\*\\Current Session",
                $"{userDataPattern}\\*\\Current Tabs",
                $"{userDataPattern}\\*\\Last Session",
                $"{userDataPattern}\\*\\Last Tabs"
            ]),
            Cleaner = NewFileDeletionCleaner(),
            SelectedByDefault = true
        };
    }

    private IEnumerable<CleaningTask> BuildFirefoxTasks()
    {
        var profilesRoot = Environment.ExpandEnvironmentVariables("%AppData%\\Mozilla\\Firefox\\Profiles");
        if (!Directory.Exists(profilesRoot))
        {
            yield break;
        }

        yield return new CleaningTask
        {
            Key = "firefox-cache",
            DisplayName = "Cache Firefox",
            Description = "Cache de navigation de Mozilla Firefox (pages, images, scripts).",
            Section = CleaningSection.Browsers,
            Category = CleanupCategory.BrowserCache,
            Scanner = new PathPatternScanner("firefox-cache", CleanupCategory.BrowserCache,
            [
                "%LocalAppData%\\Mozilla\\Firefox\\Profiles\\*\\cache2",
                "%AppData%\\Mozilla\\Firefox\\Profiles\\*\\cache2"
            ]),
            Cleaner = NewFileDeletionCleaner(),
            SelectedByDefault = true
        };

        yield return new CleaningTask
        {
            Key = "firefox-cookies",
            DisplayName = "Cookies Firefox",
            Description = "Cookies enregistrés par Firefox (déconnectera la plupart des sites).",
            Section = CleaningSection.Browsers,
            Category = CleanupCategory.BrowserCookies,
            Scanner = new PathPatternScanner("firefox-cookies", CleanupCategory.BrowserCookies,
            [
                "%AppData%\\Mozilla\\Firefox\\Profiles\\*\\cookies.sqlite",
                "%AppData%\\Mozilla\\Firefox\\Profiles\\*\\cookies.sqlite-wal",
                "%AppData%\\Mozilla\\Firefox\\Profiles\\*\\cookies.sqlite-shm"
            ]),
            Cleaner = NewFileDeletionCleaner(),
            SelectedByDefault = false
        };

        yield return new CleaningTask
        {
            Key = "firefox-formdata",
            DisplayName = "Données de formulaires Firefox",
            Description = "Auto-remplissage et données de formulaires enregistrées par Firefox.",
            Section = CleaningSection.Browsers,
            Category = CleanupCategory.BrowserFormData,
            Scanner = new PathPatternScanner("firefox-formdata", CleanupCategory.BrowserFormData,
            [
                "%AppData%\\Mozilla\\Firefox\\Profiles\\*\\formhistory.sqlite"
            ]),
            Cleaner = NewFileDeletionCleaner(),
            SelectedByDefault = false
        };

        // L'historique Firefox (places.sqlite) contient aussi les favoris : le nettoyer sans
        // risque nécessiterait des suppressions SQL ciblées, hors périmètre de cette étape.
    }

    private IEnumerable<CleaningTask> BuildThirdPartyTasks()
    {
        foreach (var definition in AppDefinitionsLoader.LoadAll())
        {
            yield return new CleaningTask
            {
                Key = definition.Key,
                DisplayName = definition.DisplayName,
                Description = definition.Description,
                Section = string.Equals(definition.Section, "cloud", StringComparison.OrdinalIgnoreCase)
                    ? CleaningSection.Cloud
                    : CleaningSection.ThirdPartyApplications,
                Category = CleanupCategory.ThirdPartyApplication,
                Scanner = new PathPatternScanner(definition.Key, CleanupCategory.ThirdPartyApplication, definition.Patterns),
                Cleaner = NewFileDeletionCleaner(),
                SelectedByDefault = definition.SelectedByDefault
            };
        }
    }
}
