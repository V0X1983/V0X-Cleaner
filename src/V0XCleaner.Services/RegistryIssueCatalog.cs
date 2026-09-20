using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.Cleaners;
using V0XCleaner.Services.Scanners;

namespace V0XCleaner.Services;

/// <summary>
/// Assemble les 8 catégories de la page Registre (Étape 2). Chaque tâche déclare explicitement
/// le nettoyeur adapté à la nature de son scan : suppression de clé entière, suppression d'une
/// valeur unique, ou suppression de fichier (raccourcis).
/// </summary>
public sealed class RegistryIssueCatalog(IRegistryBackupService backupService, ILoggerFactory loggerFactory, IPathGuard pathGuard, IQuarantineService quarantineService, ISettingsService settingsService) : IRegistryIssueCatalog
{
    public IReadOnlyList<CleaningTask> GetTasks()
    {
        var keyCleaner = new RegistryKeyDeletionCleaner(backupService, loggerFactory.CreateLogger<RegistryKeyDeletionCleaner>());
        var valueCleaner = new RegistryValueDeletionCleaner(backupService, loggerFactory.CreateLogger<RegistryValueDeletionCleaner>());
        var fileCleaner = new FileDeletionCleaner(pathGuard, quarantineService, settingsService, loggerFactory.CreateLogger<FileDeletionCleaner>());

        return
        [
            new CleaningTask
            {
                Key = "registry-file-extensions",
                DisplayName = "Extensions de fichiers",
                Description = "Extensions enregistrées pointant vers un type de fichier qui n'existe plus.",
                Section = CleaningSection.Registry,
                SubGroupLabel = "Extensions de fichiers",
                Category = CleanupCategory.Registry,
                Scanner = new OrphanedFileExtensionsScanner(),
                Cleaner = keyCleaner,
                SelectedByDefault = true
            },
            new CleaningTask
            {
                Key = "registry-com-clsid",
                DisplayName = "ActiveX et classes COM",
                Description = "Classes COM/ActiveX dont le composant (DLL/EXE) a été supprimé du disque.",
                Section = CleaningSection.Registry,
                SubGroupLabel = "ActiveX / COM",
                Category = CleanupCategory.Registry,
                Scanner = new OrphanedComClsidScanner(),
                Cleaner = keyCleaner,
                SelectedByDefault = true
            },
            new CleaningTask
            {
                Key = "registry-app-paths",
                DisplayName = "Chemins d'application",
                Description = "Chemins d'exécutables enregistrés (App Paths) pour des programmes désinstallés.",
                Section = CleaningSection.Registry,
                SubGroupLabel = "Chemins d'application",
                Category = CleanupCategory.Registry,
                Scanner = new OrphanedAppPathsScanner(),
                Cleaner = keyCleaner,
                SelectedByDefault = true
            },
            new CleaningTask
            {
                Key = "registry-shared-dlls",
                DisplayName = "DLL partagées",
                Description = "Références à des bibliothèques partagées (SharedDlls) dont le fichier n'existe plus.",
                Section = CleaningSection.Registry,
                SubGroupLabel = "DLL partagées",
                Category = CleanupCategory.Registry,
                Scanner = new OrphanedSharedDllsScanner(),
                Cleaner = valueCleaner,
                SelectedByDefault = true
            },
            new CleaningTask
            {
                Key = "registry-fonts",
                DisplayName = "Polices invalides",
                Description = "Polices enregistrées dont le fichier de police a été supprimé du dossier Fonts.",
                Section = CleaningSection.Registry,
                SubGroupLabel = "Polices",
                Category = CleanupCategory.Registry,
                Scanner = new OrphanedFontsScanner(),
                Cleaner = valueCleaner,
                SelectedByDefault = true
            },
            new CleaningTask
            {
                Key = "registry-mui-cache",
                DisplayName = "Cache MUI",
                Description = "Entrées de cache d'applications (noms/icônes) pour des programmes déjà désinstallés.",
                Section = CleaningSection.Registry,
                SubGroupLabel = "Cache MUI",
                Category = CleanupCategory.Registry,
                Scanner = new OrphanedMuiCacheScanner(),
                Cleaner = valueCleaner,
                SelectedByDefault = true
            },
            new CleaningTask
            {
                Key = "registry-uninstallers",
                DisplayName = "Désinstalleurs fantômes",
                Description = "Entrées \"Programmes et fonctionnalités\" dont le désinstalleur n'existe plus.",
                Section = CleaningSection.Registry,
                SubGroupLabel = "Désinstalleurs fantômes",
                Category = CleanupCategory.Registry,
                Scanner = new OrphanedUninstallEntriesScanner(),
                Cleaner = keyCleaner,
                SelectedByDefault = true
            },
            new CleaningTask
            {
                Key = "registry-broken-shortcuts",
                DisplayName = "Raccourcis brisés",
                Description = "Raccourcis du Bureau et du menu Démarrer pointant vers une cible qui n'existe plus.",
                Section = CleaningSection.Registry,
                SubGroupLabel = "Raccourcis brisés",
                Category = CleanupCategory.Registry,
                Scanner = new BrokenShortcutsScanner(),
                Cleaner = fileCleaner,
                SelectedByDefault = true
            }
        ];
    }
}
