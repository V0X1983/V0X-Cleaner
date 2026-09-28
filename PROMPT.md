# V0X Cleaner — Feuille de route de développement

Application Windows 11 de nettoyage et d'optimisation système, équivalente à CCleaner (version gratuite + fonctionnalités Pro/Professional), développée en **C# / .NET 8 / WPF** à l'origine — **migration en cours vers .NET 10 / C# 13 / WinUI 3 packagé MSIX**, voir la section « Migration en cours » plus bas pour l'état d'avancement.

> Ce document sert de prompt directeur étape par étape. Chaque étape peut être donnée à Claude Code (ou suivie manuellement) indépendamment, dans l'ordre. Cocher au fur et à mesure.

---

## État actuel (pour reprendre dans une nouvelle conversation)

**Étapes 0 à 9 terminées + itérations UX/fonctionnelles, tout committé** (`git log --oneline`). Build OK, 60 tests unitaires passent. Solution `V0XCleaner.sln` (C# / .NET 8 / WPF, MVVM CommunityToolkit, DI Microsoft.Extensions).

Structure : `src/V0XCleaner.Core` (modèles + interfaces), `src/V0XCleaner.Services` (implémentations : registre, fichiers, WMI, COM/Windows Update, winget, menu contextuel, services Windows), `src/V0XCleaner.App` (WPF), `tests/V0XCleaner.Tests` (xUnit, vrais dossiers temporaires, jamais le vrai registre), `installer/` (build.ps1 + Inno Setup .iss ; Inno Setup 6.7.3 est installé pour l'utilisateur dans `%LOCALAPPDATA%\Programs`, build.ps1 le détecte).

**Dépôt et publication** : `https://github.com/V0X1983/V0X-Cleaner` (public, branche par défaut `master`, remote `origin`). Release `v0.1.0` publiée avec `V0XCleaner-Setup-0.1.0.exe` (non signé). Le logiciel vérifie les mises à jour sur ce dépôt par défaut (`AppSettings.DefaultUpdateOwner/Repo`). Pousser : `gh` est installé (`C:\Program Files\GitHub CLI\gh.exe`, connecté au compte V0X1983) mais pas dans le PATH de Claude ; utiliser `git -c credential.helper= -c credential.helper='!"C:/Program Files/GitHub CLI/gh.exe" auth git-credential' push`. Ne jamais pousser ni publier sans accord explicite de l'utilisateur. Fins de ligne : `.gitattributes` force LF.

Navigation (menu latéral) : **Bilan de santé** (assistant en 3 étapes : confidentialité, espace, problèmes ; remplace l'ancienne page Accueil), **Nettoyeur** (« Nettoyage personnalisé »), **Registre**, **Outils** (Démarrage à 4 onglets [applications, tâches planifiées, menu contextuel, services Windows], Désinstalleur, Analyseur de disque, Doublons, Effaceur de disque, Restauration système, Corbeille de sécurité, Mise à jour de logiciels (winget), Mise à jour de pilotes, Optimiseur de performances avec mise en veille des processus), **Options**, **Aide**.

Style unifié (référence : captures de CCleaner fournies par l'utilisateur) : titre + phrase d'explication, onglets (`TabStyle` dans `Resources/Controls.xaml`) là où pertinent, lignes sur fond `BrushSurface`, interrupteurs (`ToggleSwitchStyle`), barre du bas avec état à gauche et boutons à droite. **Note corrigée (2026-09-28)** : la mention « pas encore refaites » ci-dessous était obsolète — vérifié directement dans les fichiers `.xaml` (`grep` du titre `FontSize="24" FontWeight="Bold"`/`TitleTextBlockStyle`), les 15 pages WPF l'ont toutes, y compris Doublons, Analyseur de disque, Effaceur de disque, Restauration système, Corbeille de sécurité, Options et Aide (probablement fait dans une session antérieure sans mettre ce paragraphe à jour). Plus rien de connu à refaire sur ce point.

Acquis importants : quarantaine/Undo des fichiers nettoyés (`IQuarantineService`, index en mémoire écrit par lots — ne jamais réécrire l'index par fichier), élévation UAC à la demande (`IElevationService`), écran de chargement plein page (`ScanProgress` + `Controls/LoadingOverlay`), sauvegardes registre `.reg` limitées aux 10 dernières, journaux dans `%AppData%\V0XCleaner\logs`.

Mise à jour de logiciels (winget) — points délicats : (1) winget n'affiche AUCUNE barre de progression quand sa sortie est redirigée : le % du téléchargement est calculé (`WingetDownloadWatcher` : Content-Length de l'URL + taille du fichier dans `%TEMP%\WinGet\<Id>.<version>`), l'installation n'a pas de % (barre animée + temps écoulé). (2) stdin de winget est fermé : sinon il attend indéfiniment une saisie (ex. Battle.net exige un dossier d'installation → message dédié). (3) `--include-unknown` est requis aussi pour `upgrade --id` (versions « Unknown », ex. Roblox). (4) Les installateurs Burn/WiX (ex. EA app) redémarraient le PC : on ajoute `--custom "/norestart"` pour ces types et `ShutdownGuard` refuse la fermeture de session pendant les mises à jour. (5) Les libellés winget sont en français avec apostrophe typographique (’) : normaliser avant de comparer.

**Pièges appris** : (1) ne PAS automatiser de clics sur les vrais boutons de modification (Réparer, Nettoyer, Supprimer, Mettre à jour) sur la machine de l'utilisateur ; captures sûres = `PrintWindow` sur sa propre fenêtre + UI Automation en lecture seule. (2) Si l'app tourne en administrateur, elle est impossible à fermer depuis Claude : demander à l'utilisateur de la fermer avant de rebuild (ou compiler avec `-o artifacts/tmpbuild`). (3) `dotnet test` ne rebuild pas le projet App : toujours `dotnet build V0XCleaner.sln`. (4) Écrire les fichiers avec l'outil Write/Edit plutôt qu'en heredoc bash ou script Python : les séquences d'échappement (retours chariot, retours à la ligne, apostrophes, caractères Unicode) sont mal transmises et cassent le code C# ; vérifier avec le build après chaque édition de ce genre. (5) Compte GitHub : ne pas contourner les restrictions ; l'authentification se fait par l'utilisateur.

**Idées restantes (hors roadmap)** : signature de code (SignPath Foundation gratuit pour l'open source, ou Azure Artifact Signing, ou certificat classique — nécessite une inscription/décision de l'utilisateur, pas automatisable depuis Claude) ; workflow GitHub Actions de build/publication ; onglets « à jour » et « ignorés » pour les mises à jour de logiciels ; multi-langue ; splash/Mica ; récupération de fichiers ; icônes réelles des programmes.

~~Refaire le style des pages restantes~~ : **déjà fait, note stale corrigée** (2026-09-28) — voir la section « Style unifié » plus haut. ~~Mettre à jour le CHANGELOG et préparer la 0.2.0~~ : section « Non publié » de `CHANGELOG.md` à jour avec les correctifs/ajouts de ce commit ; préparer réellement une version 0.2.1 (bump, tag, release GitHub) reste une action distincte, à faire avec l'accord explicite de l'utilisateur au moment de publier.

~~Vérification automatique des mises à jour au démarrage avec message dans le logiciel~~ : **fait** (WPF + WinUI). Piège évité : le bouton « Vérifier maintenant » d'Options ne fait *pas qu'une vérification* — `OptionsViewModel.CheckForUpdatesAsync` télécharge et **installe silencieusement** dès qu'une version plus récente est trouvée (élévation UAC + redémarrage de l'app). Réutiliser cette même méthode au démarrage aurait donc auto-installé des mises à jour sans jamais demander l'utilisateur — inacceptable. À la place : `MainWindowViewModel` (les deux apps) fait sa propre vérification **strictement lecture seule** 3 secondes après le démarrage (`CheckForUpdateSilentlyAsync`, appelle uniquement `IUpdateChecker.GetLatestReleaseAsync`, jamais `DownloadInstallerAsync`) et affiche un lien discret dans le menu latéral (« Nouvelle version X.Y.Z disponible ») qui ouvre la page de la release dans le navigateur — aucun téléchargement ni installation sans action explicite. Logique de comparaison de version extraite dans `V0XCleaner.Core/UpdateVersionComparer.cs` (nouveau, testé — 6 cas), réutilisée aussi par `OptionsViewModel` pour éliminer la duplication qui existait avant. `dotnet build`/`dotnet test` verts (75/75, +6). Vérifié à l'exécution (WPF et WinUI) : la vérification silencieuse se déroule sans exception dans les journaux Serilog, aucune bannière affichée puisque la version installée (0.2.0) est déjà plus récente que la dernière release GitHub publiée (0.1.0) — comportement attendu, pas testé en conditions « une vraie mise à jour existe » (nécessiterait de publier une fausse release).

~~Tri par clic sur les colonnes~~ : **fait, périmètre Désinstalleur uniquement** (WPF + WinUI — les seules colonnes réellement affichées dans un en-tête ailleurs dans l'app sont déjà couvertes par un ComboBox de tri ou n'ont pas d'en-tête visible). Les en-têtes « Programme » et « Taille » sont des `Button` transparents (template déjà `TemplateBinding`, donc `Background="Transparent"`/`BorderThickness="0"` suffisent sans nouveau style) qui basculent `SelectedSortOption` entre les deux sens du même critère ; un glyphe ▲/▼ inline (`<Run>`) indique le sens actif. Ajout de `ProgramSortMode.SizeAscending` (n'existait pas, seul « plus grand d'abord » était proposé) — apparaît aussi dans le ComboBox existant, aucune régression. `dotnet build`/`dotnet test` verts (69/69, pas de nouveau test : la logique de bascule est symétrique et triviale, et les ViewModels WPF/WinUI ne sont pas dans le périmètre testé jusqu'ici, contrairement à Core/Services).

~~Message « redémarrage nécessaire » après des mises à jour~~ : **fait** (WPF + WinUI). `SoftwareUpdater.UpdateAsync` traitait tout code de sortie winget non nul comme un échec — or 3010 (`ERROR_SUCCESS_REBOOT_REQUIRED`) et 1641 (`ERROR_SUCCESS_REBOOT_INITIATED`) sont des codes Windows Installer standard signalant un **succès** qui exige juste un redémarrage : ces mises à jour s'affichaient donc à tort comme échouées. `SoftwareUpdateResult` a un nouveau champ `RestartRequired` ; `SoftwareUpdateItemViewModel` affiche le statut en orange (`BrushWarning`, déjà défini dans les deux thèmes) avec le libellé « Mis à jour — redémarrage requis », et `UpdateSelectedAsync` adapte son message final si au moins un logiciel en a besoin. Nouveau test `SoftwareUpdater.IsRebootRequiredExitCode`. `dotnet build`/`dotnet test` verts (69/69, +5).

~~Compteur de fichiers pendant un gros nettoyage~~ : **fait** (WPF + WinUI, Nettoyeur et Registre). `Loading.Message` (écran de chargement plein page) affiche désormais la catégorie en cours et son nombre d'éléments (« Nettoyage en cours : Fichiers temporaires (1 234 élément(s))... ») au lieu du message générique fixe — visible pendant toute la durée du traitement d'une catégorie à gros volume, pas seulement la barre de progression globale par catégorie (`Loading.Progress`, inchangée). Pas de changement d'`ICleaner` (pas de progress par fichier individuel dans la boucle de suppression elle-même — 59 implémentations concernées, jugé disproportionné pour ce gain) : le compteur reflète le total scanné pour la catégorie, connu avant le nettoyage. `dotnet build`/`dotnet test` verts (64/64).

~~Avertissement Inno Setup (PrivilegesRequired=admin + clé HKCU)~~ : investigué et **clarifié plutôt que corrigé** — c'est le warning générique `UsedUserAreasWarning` d'ISCC (confirmé en compilant `installer\V0XCleaner.iss` en conditions réelles). Passer la clé en `HKA` (racine auto HKLM/HKCU) aurait été le réflexe standard, mais aurait cassé le nettoyage : `PrivilegesRequired=admin` fait que `HKA` résout toujours vers `HKLM`, alors que l'app écrit sa clé « Lancer avec Windows » dans `HKCU` sans élévation — la désinstallation nettoierait la mauvaise ruche. Gardé en `HKCU` (correct dans ce cas mono-utilisateur, l'élévation UAC ne change pas d'utilisateur), commenté dans le `.iss` pour ne pas re-suspecter un bug au prochain avertissement vu.

---

## Migration en cours : .NET 10 / C# 13 / WinUI 3 / MSIX (septembre 2026)

**Décision d'architecture** : passage de WPF+WinForms/.NET 8 vers **WinUI 3 (Windows App SDK) packagé MSIX**, gardé « à jour et durable » (demande explicite de l'utilisateur). Un paquet MSIX pur casserait plusieurs fonctionnalités cœur (élévation UAC par relance sur un chemin `WindowsApps` instable, virtualisation HKCR qui casse le nettoyage des CLSID/extensions orphelines, tâche planifiée et démarrage auto pointant sur un chemin qui change à chaque mise à jour) : la solution retenue est un **exécutable compagnon non empaqueté, full-trust** (`V0XCleaner.ElevatedHelper`, pas encore créé) installé à un chemin fixe, qui portera l'élévation/HKLM-HKCR/tâche planifiée, pendant que l'UI reste en MSIX. `V0XCleaner.Core`/`Services` gardent l'essentiel de leur code. Migration **module par module**, l'app WPF reste fonctionnelle en parallèle jusqu'à parité complète (aucune suppression avant remplacement validé). Plan détaillé (phases 0 à 8) : voir l'historique de conversation Claude du 2026-09-26 ; à reproduire ici si perdu.

**État** : **Phase 0 terminée** (socle). `V0XCleaner.Core` → `net10.0`, `V0XCleaner.Services` → `net10.0-windows`, `V0XCleaner.App` (WPF, toujours en place) → `net10.0-windows` (juste retargeté, code inchangé), `tests/V0XCleaner.Tests` → `net10.0-windows`. Nouveau projet **`src/V0XCleaner.App.WinUI`** (WinUI 3, `net10.0-windows10.0.26100.0`, `TargetPlatformMinVersion=10.0.19041.0`, `<WindowsPackageType>MSIX</WindowsPackageType>`, projet unique sans `.wapproj` séparé — scaffoldé via `dotnet new winui-navview`), ajouté à `V0XCleaner.sln`. Référence `V0XCleaner.Core`/`Services` (vérifié via `AppPaths.RootFolder` affiché dans la page « Bilan de santé » placeholder). Packages : `Microsoft.WindowsAppSDK` 2.5.1, `CommunityToolkit.Mvvm` 8.4.2, `CommunityToolkit.WinUI.Converters`/`Behaviors` 8.2.251219. Shell : `NavigationView` + `TitleBar` + `MicaBackdrop` (venant du template), un item « Bilan de santé » et « À propos ». `dotnet build V0XCleaner.sln` réussit (les deux apps + Core/Services + Tests), `dotnet test` → 60/60 tests passent. App lancée et vérifiée fonctionnelle (fenêtre « V0X Cleaner » visible, process stable).

**Piège découvert (spécifique à cette machine, potentiellement à toute machine de dev fraîche)** : `Microsoft.WindowsAppSDK` 2.5.1 crashait au lancement (`COMException 0x80040154 REGDB_E_CLASSNOTREG` dans `DeploymentManagerAutoInitializer`) parce que seul le paquet AppX principal `Microsoft.WindowsAppRuntime.2` était enregistré — il manquait les 3 paquets compagnons **Main / Singleton / DDLM**. Corrigé en les installant depuis le cache NuGet déjà téléchargé : `Add-AppxPackage` sur les 3 `.msix` dans `%USERPROFILE%\.nuget\packages\microsoft.windowsappsdk.runtime\<version>\tools\MSIX\win10-x64\` (`...DDLM.2.msix`, `...Singleton.2.msix`, `...Main.2.msix`, dans cet ordre). À refaire sur toute nouvelle machine si le même crash apparaît.

**Phase 1 terminée** (DI/Hosting + navigation + thèmes). Ajouts dans `src/V0XCleaner.App.WinUI` :
- `ViewModels/MainWindowViewModel.cs`, `NavigationItem.cs`, `PlaceholderPageViewModel.cs` : portés depuis l'ancienne app WPF (mêmes 6 entrées de nav : Bilan de santé/Nettoyeur/Registre/Outils/Options/Aide), simplifiés — plus de `CurrentPage`/cache de pages, la navigation elle-même passe par `Frame.Navigate` en code-behind (idiome WinUI 3) plutôt que par le `DataTemplate DataType=` de WPF.
- `App.xaml.cs` : bootstrap `Microsoft.Extensions.Hosting` + Serilog (fichier `v0xcleaner-winui-*.log`, même dossier que l'app WPF), `AddV0XCleanerServices()` réutilisé tel quel, thème appliqué au démarrage depuis `ISettingsService`.
- `MainWindow.xaml` : `NavigationView.MenuItemsSource` lié à `ViewModel.NavigationItems` (au lieu d'items statiques du template), `PaneFooter` avec statut d'élévation + bouton « Relancer en administrateur » (réutilise `IElevationService` tel quel — sera remplacé par l'appel au helper en Phase 2), `IsSettingsVisible="False"` (Options est déjà un item normal). `Pages/PlaceholderPage.xaml(.cs)` généré pour tout module pas encore porté (reçoit `PlaceholderPageViewModel` en paramètre de navigation) ; `Pages/AboutPage`/`SettingsPage` du template supprimées (non utilisées).
- `Resources/Theme.xaml` : palette Dark/Light portée en `ResourceDictionary.ThemeDictionaries` (mécanisme natif WinUI 3 — remplace `ThemeManager.ApplyTheme` qui rechargeait un dictionnaire à la main). `Resources/Controls.xaml` : `TabStyle`/`ToggleSwitchStyle` portés avec `VisualStateManager`/`VisualState.Setters` (WinUI 3 n'a pas `ControlTemplate.Triggers`). Les styles `Button`/`TextBox`/`ComboBox`/`ScrollBar`/`DataGrid` de l'ancien `Controls.xaml` n'ont **pas** été portés (DataGrid confirmé non utilisé nulle part dans l'app ; les autres seront portés à la demande, page par page, dans les phases suivantes plutôt que d'avance).
- `Helpers/InverseBooleanToVisibilityConverter.cs` : petit convertisseur écrit à la main plutôt que de parier sur l'API exacte du converter équivalent dans `CommunityToolkit.WinUI.Converters` (`BoolToVisibilityConverter`, API non vérifiée).

**Vérifié à l'exécution** (UI Automation en lecture seule, cf. piège n°1 du PROMPT — pas de clic sur un vrai bouton de modification, uniquement navigation) : les 6 items de menu s'affichent avec les bons libellés français, le statut d'élévation (« Mode standard ») et la version (« v0.2.0 ») s'affichent via binding, cliquer sur « Nettoyeur » navigue bien vers la page placeholder avec le bon titre/description. `dotnet build V0XCleaner.sln` + `dotnet test` (60/60) toujours verts après ces changements.

**Note pour la Phase 5 (tray icon)** : une session sœur travaillant sur un autre projet .NET10/WinUI3/MSIX (V0X Macro Recorder) a testé `H.NotifyIcon.WinUI` dans un spike jetable — fonctionne tel quel en app packagée MSIX. À privilégier plutôt que réécrire `Shell_NotifyIcon` en P/Invoke pour `TrayIconService`, sauf contre-indication trouvée en pratique.

**Phase 2 terminée** (helper d'élévation full-trust). Écart assumé par rapport au plan initial et pourquoi :

- **IPC simplifié, pas de pipe nommé** : un pipe nommé aurait exigé de contourner UIPI (un process asInvoker ne peut normalement pas se connecter à un pipe hébergé par un process élevé sans ACL/étiquette d'intégrité personnalisées — risque de sécurité si mal fait). À la place, `V0XCleaner.ElevatedHelper` est un exe **ponctuel** : lancé via `ShellExecute`/`Verb=runas` avec deux arguments (chemin d'un JSON d'entrée, chemin d'un JSON de sortie), il exécute tout le lot d'opérations reçu, écrit le résultat, et quitte. Un seul lot = une seule invite UAC, même pour plusieurs clés/valeurs à corriger d'un coup.
- **`AutoCleanScheduler` PAS traité dans cette phase** (contrairement au plan initial) : en creusant, le nettoyage planifié n'a en réalité pas besoin d'élévation (ce n'est pas le bon abstraction — mélanger "nettoyage planifié" et "opération élevée ponctuelle" aurait été une erreur de conception). Le vrai problème (chemin de tâche planifiée instable pour un exe packagé) se résout par le pattern `explorer.exe shell:AppsFolder\<PackageFamilyName>!App` (AUMID stable, contrairement au chemin `WindowsApps\...`), mais ça ne peut être vérifié qu'avec un vrai paquet MSIX installé (AUMID réel) — reporté à la Phase 6/7 plutôt que deviné à l'aveugle maintenant.

Ajouts :
- **`V0XCleaner.Core`** : `Models/Elevation/` (`ElevatedRegistryOperation`, `ElevatedOperationResult`, `ElevatedBatchRequest`/`Response`), `Abstractions/IElevatedOperationClient.cs`.
- **`V0XCleaner.Services`** : `Elevation/NullElevatedOperationClient.cs` (implémentation par défaut, `IsSupported=false`, enregistrée dans `AddV0XCleanerServices()` — **l'app WPF ne câble rien d'autre, comportement 100% inchangé**), `Elevation/ElevatedHelperLauncher.cs` (déploie une copie du helper vers un chemin fixe `%AppData%\V0XCleaner\helper\`, limite connue : la recherche du binaire source remonte jusqu'à `V0XCleaner.sln` puis cherche sous `src/V0XCleaner.ElevatedHelper/bin` — ne marche qu'en dev, la Phase 7 packaging devra fournir un vrai mécanisme), `Elevation/ProcessElevatedOperationClient.cs` (implémentation réelle).
- **`RegistryKeyDeletionCleaner`/`RegistryValueDeletionCleaner`** : tentative directe d'abord (comportement identique à avant) ; les éléments qui échouent par manque de droits sont regroupés et retentés **en un seul appel** au helper élevé si `elevatedClient.IsSupported` (sinon échec silencieux comme avant — zéro régression WPF). `RegistryIssueCatalog`/`InstalledProgramsCatalog` mis à jour pour injecter `IElevatedOperationClient`.
- **Nouveau projet `src/V0XCleaner.ElevatedHelper`** : exe console (`OutputType=WinExe` pour ne pas flasher de fenêtre), non empaqueté, `net10.0-windows`, `app.manifest` avec `requestedExecutionLevel="requireAdministrator"`. `Program.cs` lit le JSON d'entrée, exécute chaque opération (`DeleteSubKeyTree`/`DeleteValue` via `Microsoft.Win32.Registry` directement, sans dépendre de `Services` pour limiter la surface du process full-trust), écrit le JSON de sortie, code de sortie 0/1.
- **`src/V0XCleaner.App.WinUI`** : `App.xaml.cs` enregistre `ProcessElevatedOperationClient` en remplacement du `NullElevatedOperationClient` par défaut. `MainWindowViewModel` : le bouton d'élévation ne relance plus tout le process (`IElevationService.RelaunchElevated()` — cassé pour un packaged asInvoker) mais déclenche un aller-retour à vide (`ExecuteAsync([])`) vers le helper pour confirmer que l'UAC fonctionne ; libellé « Vérifier l'élévation », statut `IsHelperElevated`/`ElevationStatusLabel`.
- **Tests** : `tests/V0XCleaner.Tests/ElevationTests.cs` — round-trip JSON des modèles, `NullElevatedOperationClient` ne lance jamais rien, et surtout `ElevatedHelperLauncher.TryEnsureDeployed()` **testé pour de vrai** (pas mocké) : au moment d'écrire ceci, le test a réellement trouvé et copié `V0XCleaner.ElevatedHelper.exe` + dépendances vers `%AppData%\V0XCleaner\helper\`, confirmé par inspection directe du dossier.

**Round-trip UAC réel confirmé par l'utilisateur** (clic sur « Vérifier l'élévation » + acceptation de l'invite UAC, impossible à automatiser depuis Claude car le prompt UAC s'affiche sur le bureau sécurisé) : statut affiché « Élévation confirmée ». Phase 2 entièrement close.

**Phase 3 terminée** (les 9 pages « simples » : Bilan de santé réel, Options, Aide, Démarrage, Corbeille de sécurité, Restauration système, Mises à jour logiciels, Pilotes, Optimiseur). Élévation inchangée partout (comportement WPF conservé à l'identique — message + bouton « relancer en admin » via `IElevationService.IsElevated`, aucune de ces 9 pages ne route ses opérations privilégiées via le helper de la Phase 2 ; ce n'est pas une régression, juste hors périmètre de cette phase).

Infrastructure transverse ajoutée d'abord (sans quoi aucune des 9 pages n'aurait pu être branchée proprement) :
- `App.xaml.cs` expose désormais `App.Services` (résolution DI) et `App.MainWindow` (pour `ApplyTheme`/HWND) — chaque nouvelle page résout son ViewModel dans son constructeur via `App.Services.GetRequiredService<T>()`, contrairement au `PlaceholderPage` de la Phase 1 qui ne le faisait pas.
- `Helpers/DialogHelper.cs` (`ConfirmAsync`/`NotifyAsync` sur `ContentDialog`) remplace `MessageBox.Show` (absent de WinUI) sur les 7 pages qui en avaient besoin.
- `Controls/LoadingOverlay.xaml(.cs)` porté (anneau `Storyboard`/`RotateTransform` au lieu de `BeginAnimation`, absent de WinUI), `ViewModels/ScanProgress.cs` porté à l'identique (agnostique UI).
- `Resources/Controls.xaml` : ajout de `Button` (retemplaté, VisualStateManager) et `PrimaryButtonStyle` (bouton d'action accent, utilisé par la quasi-totalité des 9 pages), `CheckBox`/`ToolTip` (simples surcharges de propriété, pas de retemplate). **`TextBox` n'est pas retemplaté** (risque de casser des parties nommées requises par le contrôle WinUI, ex. `PlaceholderTextContentPresenter`) : à la place, `Resources/Theme.xaml` surcharge les clés de thème `TextControl*` (Background/BorderBrush/Foreground/PlaceholderForeground par état) que le `TextBox` natif consomme déjà — écart assumé par rapport au plan initial (qui prévoyait un retemplate comme pour WPF), gardé pour les phases suivantes si un besoin réel apparaît. `ComboBox`/`ScrollBar` toujours pas retemplatés (habillage Fluent natif déjà correct).
- Convertisseurs manquants : `BoolToVisibilityConverter`/`BoolNegationConverter`/`StringVisibilityConverter` de `CommunityToolkit.WinUI.Converters` (déjà référencé, jamais utilisé) réutilisés sous les clés `BooleanToVisibilityConverter`/`InverseBooleanConverter`/`NullOrEmptyToVisibilityConverter` — pas besoin de les réécrire à la main comme prévu au départ (seul `FilePathToFileNameConverter` est resté un converter maison, sans équivalent direct).
- `Infrastructure/ShutdownGuard.cs` porté (P/Invoke `ShutdownBlockReasonCreate`, `WindowInteropHelper` → `WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow)`).

Écart d'architecture découvert en cours de route (pas anticipé dans le plan initial) : **« Outils » est en réalité une coquille de sous-navigation** (liste à gauche + zone de contenu, comme l'onglet WPF `ToolsPageView`), pas 9 items de navigation à plat dans le `NavigationView` principal. `Pages/ToolsPage.xaml(.cs)` la reproduit avec un `Frame` imbriqué (`ToolFrame`) et un switch par clé dans le code-behind (même idiome que `MainWindow.xaml.cs`, pas de sélection de template par type CLR comme en WPF) — 6 des 10 items du menu Outils sont branchés (Démarrage, Restauration système, Corbeille de sécurité, Mises à jour logicielles, Pilotes, Optimiseur) ; les 4 restants (Désinstalleur, Analyseur de disque, Doublons, Effaceur de disque) retombent sur `PlaceholderPage`, hors périmètre de cette phase.

Autre pattern remplacé au fil des pages : le binding WPF `Command="{Binding DataContext.XCommand, RelativeSource={RelativeSource AncestorType=ItemsControl}}"` (pour qu'une ligne de liste appelle une commande du ViewModel parent) **n'a pas d'équivalent direct en WinUI**. Remplacé partout par un gestionnaire `Click` en code-behind + `Tag="{Binding}"` sur le bouton (`sender is Button { Tag: XViewModel item }` puis `ViewModel.XCommand.Execute(item)`) — plus simple qu'une injection de commande dans le ViewModel de ligne, et déjà le pattern utilisé par l'ancien `SystemRestoreView` WPF pour son bouton Supprimer.

Autres remplacements WinUI systématiques : `ContextMenu`/`MenuItem` dynamiques (StartupManager, Optimizer) → `MenuFlyout`/`MenuFlyoutItem` + `ShowAt(button)` ; `DockPanel` (absent de WinUI) → `Grid` à colonnes `*`/`Auto` équivalent, partout où il apparaissait (StartupManager, SoftwareUpdates, Optimizer, HealthCheck) ; `Style.Triggers`/`DataTrigger` (absent de WinUI) → soit une propriété calculée sur le ViewModel liée directement (ex. `ToggleColumnHeaderText`, `ColumnHeaderText`), soit deux `TextBlock`/`Ellipse` dupliqués avec `Visibility` inversée quand seule la couleur `ThemeResource` change (ex. statut d'erreur des mises à jour, jauge d'état du Bilan de santé, points d'impact de l'Optimiseur) — plus sûr qu'un converter résolvant un `ThemeResource` en code (pas d'accès simple au thème effectif depuis un `IValueConverter` WinUI) ; `IsEnabled` **n'existe pas sur `Grid`/`StackPanel`** en WinUI (contrairement à WPF où `UIElement.IsEnabled` s'applique à tout) — déplacé sur chaque contrôle interactif concerné (Options, StartupManager) ; glyphes `Segoe MDL2 Assets` → `Segoe Fluent Icons` (mêmes points de code U+E7xx/U+E8xx, vérifiés octet par octet depuis le UTF-8 source pour le Bilan de santé, qui les passe en constructeur plutôt qu'en XAML) ; `System.Windows.Threading.DispatcherTimer` (SoftwareUpdates) → `Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().CreateTimer()` ; `Process.Start(url)` (SoftwareUpdates, Options, Drivers) → `Windows.System.Launcher.LaunchUriAsync` ; `Process.Start(dossier)` (Aide) → `Windows.System.Launcher.LaunchFolderPathAsync` ; `System.Windows.Application.Current.Shutdown()` (Options, fin d'auto-update) → `Microsoft.UI.Xaml.Application.Current.Exit()`. Tous les ViewModels utilisent la syntaxe `[ObservableProperty] public partial T X { get; set; }` (pas de champ `_x`), cohérente avec la Phase 1/2 — pour les lignes qui doivent revenir en arrière sans re-déclencher leur propre `OnXChanged` (`StartupEntryViewModel`, `ToggleEntryViewModel`), un booléen `_suppressApply` remplace l'écriture directe du champ généré (impossible avec la syntaxe partial-property).

`ToolsPageViewModel` (WinUI) ne construit plus les 10 ViewModels enfants d'avance comme en WPF : chaque page Outils résout son propre ViewModel via DI au moment de la navigation (`ToolFrame.Navigate`), plus simple et plus proche de l'idiome WinUI déjà en place pour le `NavigationView` principal.

`Pages/HomePage.xaml(.cs)` (placeholder de la Phase 0, affichait juste `AppPaths.RootFolder`) supprimé, remplacé par `Pages/HealthCheckPage.xaml(.cs)` sur la même clé de navigation `health-check`.

**Icône de zone de notification (Options → « Surveillance en temps réel »)** : le réglage est exposé et persisté (`ISettingsService`) mais reste sans effet visible — `TrayIconService` (WinForms `NotifyIcon`) n'est pas porté, toujours réservé à la Phase 5 (`H.NotifyIcon.WinUI`, cf. note plus haut). Pas une régression : cette icône n'existe pas encore côté WinUI.

`dotnet build V0XCleaner.sln` et `dotnet test` verts (64/64, +4 depuis la Phase 2) après le port des 9 pages.

**Piège résolu (pas un problème de session ni de machine — un réglage du projet)** : double-cliquer sur `V0XCleaner.exe` (`bin/Debug/net10.0-windows10.0.26100.0/win-x64/`) plantait de façon reproductible, aussi bien pour l'utilisateur que depuis le terminal de Claude, même après réinstallation des paquets compagnons et un redémarrage complet de la machine — l'hypothèse « session non interactive » de la première investigation était fausse. Cause réelle, trouvée en lisant les `.targets` MSBuild du SDK (`microsoft.windowsappsdk.foundation`) :
- `EnableMsixTooling=true` implique `WindowsPackageType=MSIX`, ce qui active automatiquement `WindowsAppSdkDeploymentManagerInitialize` → compile un vérificateur statique (`DeploymentManagerAutoInitializer`) qui s'exécute avant tout code applicatif et plante en `COMException 0x80040154 REGDB_E_CLASSNOTREG` tant que l'exe n'a pas d'identité de paquet réelle (double-clic sur le .exe brut de `bin/Debug`).
- Même ce vérificateur désactivé, `Microsoft.UI.Xaml.Application.Start()` lui-même échoue de la même façon : l'activation WinRT du Windows App SDK par un exe non empaqueté exige soit une vraie identité de paquet (installation MSIX), soit l'identité de debug/AUMID que seul `dotnet run` enregistre (via `Microsoft.Windows.SDK.BuildTools.WinApp`, cf. commentaire du `.csproj`) — jamais un simple double-clic.

**Correctif dans `V0XCleaner.App.WinUI.csproj`** : `WindowsAppSdkDeploymentManagerInitialize=false` (désactive le vérificateur, pas nécessaire en dev) + `WindowsAppSDKSelfContained=true` **en configuration Debug uniquement** (copie les DLL du Windows App SDK directement à côté de l'exe — plus besoin d'identité de paquet ni de paquet AppX installé/enregistré sur la machine). Confirmé : un simple `dotnet build` puis double-clic sur `bin/Debug/net10.0-windows10.0.26100.0/win-x64/V0XCleaner.exe` fonctionne maintenant de façon fiable, fenêtre « V0X Cleaner » visible. **Limité à Debug** : un vrai paquet MSIX (Phase 7) ne doit pas être autonome (dépendre du framework partagé est le fonctionnement normal d'un paquet MSIX) — à revalider à ce moment-là. `dotnet build`/`dotnet test` toujours verts (64/64) après ce changement.

**Phase 3 vérifiée visuellement par l'utilisateur** : les 9 pages (Bilan de santé, Outils › Démarrage/Restauration système/Corbeille de sécurité/Mises à jour logicielles/Pilotes/Optimiseur, Options, Aide) affichent bien du contenu réel (pas de placeholder). Confirmé aussi que les 4 items d'Outils hors périmètre (Désinstalleur, Analyseur de disque, Doublons, Effaceur de disque) et les items de menu principal Nettoyeur/Registre restent des placeholders comme attendu (pas encore portés). Phase 3 entièrement close. Rien n'est committé à ce stade (comme convenu, jamais sans accord explicite).

**Phase 4 terminée** (Nettoyeur + Registre, les deux modules cœur — items de menu principal `cleaner`/`registry`). Plus simples que les pages les plus complexes de la Phase 3 : aucun `TreeView`/`DataGrid` (juste des `ItemsControl` imbriqués section→tâches, motif déjà maîtrisé), aucun binding `RelativeSource AncestorType`, aucun `ContextMenu`, aucun glyphe Segoe MDL2. Seuls contournements nécessaires : `MessageBox.Show` → `DialogHelper` (confirmation avant nettoyage/réparation/restauration), `DockPanel` → `Grid`, et plusieurs bindings `StringFormat='...{0}...'` (non supportés en WinUI) remplacés par des propriétés calculées (`RecoverableLabel`, `TotalIssuesLabel`, `FoundItemsLabel` sur `CleaningTaskViewModel`).

**ViewModels partagés portés une seule fois** (`CleaningSectionViewModel`, `CleaningTaskViewModel`, `CleaningTaskStatus`), réutilisés par `CleanerPageViewModel` et `RegistryPageViewModel` — évite la duplication, comme en WPF. `CleaningTaskViewModel` garde la sémantique d'origine ; l'assignation initiale `IsSelected = task.SelectedByDefault` dans le constructeur passe par le setter de la propriété partial (pas de risque de boucle ici, contrairement à `StartupEntryViewModel`/`ToggleEntryViewModel` en Phase 3, car aucun `OnIsSelectedChanged` n'existe sur ce type).

**Élévation confirmée gratuite pour Registre** : `RegistryPageViewModel` n'appelle jamais `IElevationService`/`IElevatedOperationClient` — il consomme `IRegistryIssueCatalog.GetTasks()`, dont les `Cleaner` (`RegistryKeyDeletionCleaner`/`RegistryValueDeletionCleaner`, déjà portés en Phase 2) routent déjà les échecs de permission HKLM vers le helper élevé. Aucune modification nécessaire côté WinUI sur ce point — confirmé par le plan avant implémentation, pas juste supposé.

**Piège découvert (à ajouter aux pièges appris généraux)** : après avoir lancé `V0XCleaner.exe`, tout rebuild suivant de `V0XCleaner.App.WinUI` échoue silencieusement à copier le nouvel exe (`MSB3027`, fichier verrouillé par le process en cours) **si le process n'est pas fermé d'abord** — mais un `dotnet build` qui échoue sur CETTE étape précise peut être manqué si on ne regarde pas la sortie en détail (le message d'erreur est noyé dans des tentatives de nouvelle tentative). Résultat observé : l'ancien binaire continue de tourner avec l'ancien comportement (une page routée en placeholder alors que le code source routait déjà vers la vraie page), ce qui ressemble à tort à un bug de routage. **Toujours fermer `V0XCleaner.exe` (`Stop-Process`) avant de rebuild**, et vérifier que `dotnet build` affiche bien « 0 Erreur(s) » (pas juste l'absence du mot ÉCHEC) avant de relancer pour une vérification utilisateur.

**Phase 4 vérifiée visuellement par l'utilisateur** : Nettoyeur (catégories Système/Navigateurs/Applications tierces/Stockage cloud avec cases à cocher) et Registre (catégories de problèmes + sélecteur de sauvegardes .reg) affichent du contenu réel. `dotnet build`/`dotnet test` verts (64/64). Rien n'est committé à ce stade (comme convenu).

**Phase 5 terminée** (icône de zone de notification, branchement réel du réglage « Surveillance en temps réel » laissé sans effet en Phase 3). `H.NotifyIcon.WinUI` 2.4.1 ajouté à `V0XCleaner.App.WinUI.csproj`, confirmant la note de la session sœur (V0X Macro Recorder).

- **`App.xaml`** : `TaskbarIcon` déclaré comme ressource applicative (`x:Key="TrayIcon"`, jamais placé dans une fenêtre — WinUI 3 n'a pas de racine implicite pour héberger une icône hors fenêtre, contrairement à `System.Windows.Application` en WPF). Pas d'`IconSource` en XAML : **piège découvert en pratique**, la conversion WinRT `ms-appx:///` échoue silencieusement (aucune exception, `TaskbarIcon.Icon` reste `null`, `ForceCreate()` réussit quand même et `IsCreated=true`) tant que l'app n'a pas d'identité de paquet réelle — exactement le même piège que la `DeploymentManagerAutoInitializer` de la Phase 0, mais cette fois sans erreur visible : l'icône est juste invisible. Détecté uniquement en ajoutant un log explicite de `TrayIcon.Icon` après `ForceCreate()`. Contournement : `Infrastructure/TrayIconService.cs` charge l'icône directement depuis le disque (`System.Drawing.Icon`, chemin `AppContext.BaseDirectory/Assets/AppIcon.ico`), assigné à `TaskbarIcon.Icon` en code, comme le faisait déjà `TrayIconService` côté WPF avec un flux de ressource. Nécessite `<Content Include="Assets\AppIcon.ico"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></Content>` dans le `.csproj` (absent par défaut : les `Content` du template WinUI sont prévus pour le manifeste MSIX, pas copiés à côté de l'exe en debug non empaqueté).
- **`Infrastructure/TrayIconService.cs`** (nouveau, WinUI) : porte le service WPF équivalent. `ApplySettings()` appelle `TaskbarIcon.ForceCreate(enablesEfficiencyMode: false)` **une seule fois pour toute la durée de vie du process** (idempotent via un booléen `_created`) à la première activation. **Piège découvert en pratique** (pas dans le plan initial) : désactiver puis réactiver la surveillance dans la même session en rappelant `Dispose()`/`ForceCreate()` à chaque bascule casse tout — un second `ForceCreate()` après un `Dispose()` échoue avec `System.InvalidOperationException: TryCreate failed` (constaté en testant explicitement le cycle activer→désactiver→réactiver sans relancer l'app, un scénario que la Phase 3/4 n'avait pas besoin de couvrir). Corrigé en basculant `TaskbarIcon.TrayIcon.Show()`/`Hide()` (le `H.NotifyIcon.Core.TrayIcon` sous-jacent, réversible) pour toutes les activations/désactivations **après** la création initiale — même sémantique que `NotifyIcon.Visible` côté WPF. Le vrai `Dispose()` n'a lieu qu'à la fermeture de l'app (`Dispose()` de `TrayIconService`, appelé par `App.CompleteShutdown()`). Timer `System.Threading.Timer` identique à WPF pour `CheckJunkAsync` (notifie via `ShowNotification` si le seuil est dépassé). Menu contextuel (`MenuFlyout` : Ouvrir/Nettoyage rapide/Quitter) et double-clic (`DoubleClickCommand`, `CommunityToolkit.Mvvm.Input.RelayCommand` — H.NotifyIcon expose des `ICommand`, pas d'événements routés directs comme en WPF).
- **Fermeture masquée** : `MainWindow.xaml.cs` s'abonne à `AppWindow.Closing` (cancelable, disponible depuis WinAppSDK 1.3+ — absent de l'API `Window.Closing` classique WPF) ; si la surveillance est active, annule et appelle `AppWindow.Hide()` ; sinon délègue à `App.Shutdown()`.
- **Point de sortie unique `App.Shutdown()`** : écart assumé par rapport à WPF. `Microsoft.UI.Xaml.Application.Exit()` ne garantit pas le déclenchement de `Window.Closed` sur les fenêtres restantes (contrairement à `Application.Current.Shutdown()` en WPF, qui passe explicitement par `OnExit`) — le nettoyage (dispose de `TrayIconService`, `IHost`, flush Serilog) est donc fait explicitement dans une méthode idempotente (`_isShuttingDown`) appelée à la fois par le bouton « Quitter » du menu et par `MainWindow` quand la fermeture normale doit vraiment terminer le process, **avant** d'appeler `Exit()`.
- **`OptionsViewModel`** : `TrayIconService` injecté, `SaveAsync()` appelle `_trayIconService.ApplySettings()` après la sauvegarde (même pattern que `App.MainWindow.ApplyTheme(SelectedTheme)` juste au-dessus). Le commentaire qui indiquait le réglage « sans effet visible » (Phase 3) est retiré.

**Vérifié à l'exécution** (build x64 direct, `bin/x64/Debug/.../win-x64/V0XCleaner.exe`, cf. piège ci-dessous sur le dossier de sortie) :
- Activer la surveillance dans Options + Enregistrer → log `Icône de zone de notification créée (IsCreated=True)`, aucune exception, `TrayIcon.Icon` non nul après le correctif ci-dessus.
- Fermeture de la fenêtre (`WM_CLOSE` simulé) pendant que la surveillance est active → fenêtre masquée (`IsWindowVisible=False`) mais process **toujours vivant** (confirmé par `Get-Process`).
- Surveillance désactivée + Enregistré, puis fermeture de la fenêtre → process se termine proprement (log flushé, plus dans `tasklist`), comme avant la Phase 5.
- Cycle activer→désactiver→réactiver (via automation UI, sans relancer l'app) répété plusieurs fois de suite → plus aucune exception après le correctif `Show()`/`Hide()` (contre `TryCreate failed` systématique avant correctif).
- **Icône confirmée présente** (après investigation plus poussée) : introuvable au premier abord car `Shell_TrayWnd`/`TrayNotifyWnd` ne l'expose pas directement — la vraie zone de notification Windows 11 est un XAML Island séparé (`TopLevelWindowForOverflowXamlIsland`, trouvé par énumération de toutes les fenêtres top-level, classe absente de la doc classique `Shell_NotifyIcon`/`NotifyIconOverflowWindow`). Ouvert via le chevron « Afficher les icônes cachées », son arbre UI Automation montre bien `ControlType.Button :: 'V0X Cleaner'` avec un `ControlType.Image` enfant (22x22) juste avant les icônes de V0X Macro Recorder/Claude — l'icône est donc correctement enregistrée, nommée et pourvue d'une image réelle, simplement rangée par défaut dans le tiroir « icônes cachées » (comportement standard de Windows 11 pour toute nouvelle icône, l'utilisateur peut la glisser vers la zone toujours visible). Le rendu pixel exact reste invisible via `PrintWindow` (motif hachuré, même limite que pour les icônes système natives voisines dans la même capture — composition DirectComposition/XAML Islands non capturable dans cette session distante, pas un défaut de l'icône). Le clic droit (menu contextuel) n'a pas pu être déclenché par automatisation (les clics `mouse_event` synthétisés n'atteignent pas cette zone de façon fiable ici) — **reste à confirmer visuellement par l'utilisateur**, comme le round-trip UAC de la Phase 2.

**Piège découvert (à ajouter aux pièges généraux)** : `dotnet build V0XCleaner.sln` sans plateforme explicite produit `bin/x86/Debug/.../win-x86/V0XCleaner.dll` dans cette session (probablement l'architecture par défaut de l'agent), alors que l'exe précédemment lancé et vérifié provenait de `bin/Debug/.../win-x64/` (build antérieur, plateforme non précisée). Pour rebuild puis relancer le même exe `win-x64` de façon fiable : `dotnet build src/V0XCleaner.App.WinUI/V0XCleaner.App.WinUI.csproj -p:Platform=x64`, qui sort dans `bin/x64/Debug/.../win-x64/`. Bien vérifier le chemin de l'exe qu'on relance après un rebuild plutôt que de supposer qu'il correspond au dernier `dotnet build` sans plateforme.

`dotnet build V0XCleaner.sln` / `dotnet test` toujours verts (64/64) après la Phase 5. **Committé et poussé** (commit `dd27b07`, phases 0 à 5 en un seul commit — accord explicite donné par l'utilisateur après la Phase 5).

**Phase 6/7 terminée** (identité de paquet réelle, sans certificat ni magasin de confiance modifié — accord explicite de l'utilisateur pour rester sur un **enregistrement "loose"** local, `Add-AppxPackage -Register`, en écartant l'option "vrai .msix signé installable par double-clic" qui aurait touché `Cert:\LocalMachine\TrustedPeople`). Résout le point laissé ouvert en Phase 2 : `AutoCleanScheduler` ciblait `Environment.ProcessPath`, un chemin `WindowsApps\<Nom>_<Version>_...` qui change à chaque mise à jour du paquet et casserait la tâche planifiée.

- **`Package.appxmanifest`** : `DisplayName`/`uap:VisualElements` corrigés (« V0X Cleaner » au lieu de « V0XCleaner.App.WinUI », description réelle) ; `systemai:Capability Name="systemAIModels"` retiré (résidu du template `dotnet new winui-navview`, jamais utilisé). Ajout d'un **alias d'exécution stable** (`windows.appExecutionAlias`, `uap5:ExecutionAlias Alias="v0xcleaner.exe"` — **piège** : le schéma `uap3` de cette extension exige un `<uap3:ExecutionAliasChoice>` intermédiaire, `Add-AppxPackage` refuse sinon avec une erreur de validation de schéma ; `uap5` accepte `<uap5:ExecutionAlias>` directement, sans wrapper). Une fois le paquet installé, Windows crée `%LOCALAPPDATA%\Microsoft\WindowsApps\v0xcleaner.exe` : un chemin fixe, indépendant de la version du paquet, qui relaie la ligne de commande vers l'app empaquetée (activation `CommandLineLaunch`) — contrairement à `explorer.exe shell:AppsFolder\<PFN>!App`, qui ouvre l'app mais **ne transmet aucun argument**.
- **`V0XCleaner.Services/Native/PackageIdentity.cs`** (nouveau) : `TryGetFamilyName()` via `GetCurrentPackageFamilyName` (kernel32, P/Invoke direct) plutôt que l'API WinRT `Windows.ApplicationModel.Package` — évite d'ajouter une dépendance WinRT à ce projet (`net10.0-windows` sans version de plateforme cible) pour une simple détection empaqueté/pas empaqueté ; fonctionne identiquement pour l'app WPF (jamais empaquetée, retourne toujours `false`, zéro régression) et l'app WinUI.
- **`AutoCleanScheduler`** : la tâche planifiée cible `%LOCALAPPDATA%\Microsoft\WindowsApps\v0xcleaner.exe` (via `PackageIdentity.TryGetFamilyName()`) au lieu de `Environment.ProcessPath` quand le process est empaqueté ; sinon comportement WPF inchangé. Les arguments `--silent --clean` restent identiques dans les deux cas.
- **`App.xaml.cs` (WinUI)** : détecte l'activation `ExtendedActivationKind.CommandLineLaunch` (`Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs()` — l'argument `LaunchActivatedEventArgs` du paramètre d'`OnLaunched` ne porte PAS la ligne de commande pour ce type d'activation, contrairement à ce qu'on pourrait attendre par analogie avec WPF). Si `Operation.Arguments` contient `--silent` et `--clean`, exécute `RunSilentCleanAndExitAsync()` (port fidèle du `RunSilentCleanAndExitAsync` de l'app WPF : parcourt `ICleaningCatalog.GetTasks().Where(SelectedByDefault)`, nettoie, log les échecs) **sans jamais créer `MainWindow`**, puis `Shutdown()`.
- **`V0XCleaner.App.WinUI.csproj`** : `WindowsAppSdkDeploymentManagerInitialize` conditionné à `Debug` uniquement (`false`) au lieu d'être toujours `false` — une vraie configuration Release/packagée en a besoin pour installer automatiquement le Windows App SDK partagé s'il manque sur la machine cible (le vérificateur qui plantait en double-clic non empaqueté, cf. piège Phase 0, n'a plus de raison d'être désactivé une fois l'app réellement empaquetée).

**Vérifié à l'exécution** (enregistrement loose du build `bin/x64/Debug/.../win-x64/`, Developer Mode déjà actif sur la machine — `AllowDevelopmentWithoutDevLicense=1` — aucune modification apportée à ce réglage) :
- `Add-AppxPackage -Register AppxManifest.xml` → paquet réellement installé (`Get-AppxPackage` le confirme, `PackageFamilyName` réel). **Piège** : re-registrer vers un nouveau dossier de sortie pour la même Identity/Version ne déplace pas l'InstallLocation (Windows semble ignorer silencieusement le changement de chemin à version égale) — il faut `Remove-AppxPackage` puis re-`Add-AppxPackage -Register` pour que le nouveau chemin soit pris en compte.
- Lancement via `explorer.exe shell:AppsFolder\<PackageFamilyName>!App` → fenêtre « V0X Cleaner » s'ouvre normalement.
- Activer « Nettoyage automatique » dans Options + Enregistrer (app lancée avec identité de paquet réelle) → tâche planifiée Windows inspectée directement (`Get-ScheduledTask`) : `Execute = %LOCALAPPDATA%\Microsoft\WindowsApps\v0xcleaner.exe`, `Arguments = --silent --clean` — confirme que `PackageIdentity`/`AutoCleanScheduler` ciblent bien l'alias, pas un chemin `WindowsApps\...` versionné.
- Invocation directe de l'alias avec `--silent --clean` → le process démarre (log Serilog « Application started »), **aucune fenêtre créée**, aucune erreur, process terminé proprement (absent de `tasklist` après quelques secondes) — confirme `CommandLineLaunch` + `RunSilentCleanAndExitAsync` de bout en bout.
- **Piège corrigé en cours de route** : `TitleBar.IconSource="Assets\AppIcon.ico"` affichait une icône « image cassée » **même avec une vraie identité de paquet** — donc pas uniquement un problème d'identité de paquet comme supposé en Phase 5, mais une limite du décodeur `BitmapImage`/WIC pour le format `.ico` dans ce contrôle XAML précis (`AppWindow.SetIcon`, Win32, gère le même `.ico` sans problème pour l'icône de fenêtre/barre des tâches). Remplacé par un `.png` (`Assets/Square44x44Logo.scale-200.png`) : n'affiche plus d'icône cassée, mais reste actuellement vide (zone blanche) — **pas résolu, cosmétique, laissé en l'état** (aucun impact fonctionnel, l'icône de fenêtre/barre des tâches via `AppWindow.SetIcon` fonctionne correctement).

`dotnet build`/`dotnet test` verts (64/64) après la Phase 6/7. Paquet loose-enregistré laissé en place sur la machine de dev (désinstallable avec `Remove-AppxPackage` si besoin ; aucun certificat ni magasin de confiance touché). **Committé et poussé** (commit `b9e4acb`).

**Phase 8 terminée** (les 4 derniers modules Outils : Désinstalleur, Analyseur de disque, Doublons, Effaceur de disque — tous les items du menu Outils sont maintenant portés, plus aucun `PlaceholderPage` dans cette section). Ordre traité du plus simple au plus complexe.

- **Effaceur de disque** (simple) : `DriveWiperViewModel`/`WipeMethodOption` portés à l'identique (juste la syntaxe `[ObservableProperty] public partial T X { get; set; }`). `IsEnabled` déplacé du `StackPanel` "Méthode" vers le `ComboBox` lui-même (Panel n'a pas `IsEnabled` en WinUI, piège déjà connu).
- **Doublons** : `DuplicateFinderViewModel`/`DuplicateGroupViewModel`/`DuplicateFileEntryViewModel` portés. `StringFormat='Sélectionné : {0}'` (non supporté) remplacé par une propriété calculée `SelectedLabel`. Premier module à avoir besoin d'un sélecteur de dossier WinUI (`Windows.Storage.Pickers.FolderPicker`) : nouveau `Helpers/PickerHelper.cs`, réutilisé ensuite par le Désinstalleur — **piège** : contrairement à `Microsoft.Win32.OpenFolderDialog` (WPF, autonome), `FolderPicker`/`FileSavePicker` exigent une association explicite à un HWND (`WinRT.Interop.InitializeWithWindow.Initialize`) avant affichage, sans quoi ils lèvent une exception. Limitation acceptée : pas d'équivalent WinRT à `InitialDirectory` (dossier de départ suggéré), seulement une valeur parmi l'enum `PickerLocationId` — la nuance WPF « s'ouvre sur le dossier actuel du programme » (Désinstalleur → Déplacer) est perdue, non bloquant.
- **Analyseur de disque** : `DiskAnalyzerViewModel`/`FolderSizeNodeViewModel` portés. Pas de vrai treemap (la version WPF n'en a jamais eu : juste une barre proportionnelle simple, `FractionToWidthConverter`) — remplacé par une propriété calculée `BarWidth` plutôt qu'un converter. `MouseBinding`/`InputBindings` (absents de WinUI) sur le double-clic pour ouvrir un dossier remplacés par l'événement `DoubleTapped` ; les bindings de commande `RelativeSource AncestorType=UserControl` (bouton "Explorer") remplacés par le pattern `Tag="{Binding}"` + `Click` déjà établi.
- **Désinstalleur** (le plus complexe) : `UninstallManagerViewModel`/`InstalledProgramViewModel`/`ProgramSortOption` portés. **Écart réel** : WPF convertit une icône `System.Drawing.Icon` en `ImageSource` directement (`Imaging.CreateBitmapSourceFromHIcon`, un HICON brut) — WinUI n'a pas d'équivalent direct. Contournement : l'icône est réencodée en PNG en mémoire (`Icon.ToBitmap()` + `Bitmap.Save(MemoryStream, ImageFormat.Png)`, thread d'arrière-plan), puis chargée via `BitmapImage.SetSourceAsync` sur un `InMemoryRandomAccessStream` (doit s'exécuter sur le thread UI, contrairement à l'extraction/l'encodage qui reste dans `Task.Run`). Utilise aussi `PickerHelper` pour "Déplacer" (dossier) et l'export CSV (`FileSavePicker`). `DockPanel` (×2, ligne de programme + barre du bas) remplacé par `Grid` à colonnes fixes/`*` explicites.

**Piège transverse découvert en testant** (pas un problème de code de cette phase, un comportement WinUI générique) : un `ComboBox` dont `SelectedItem` est affecté par code **après** que son `ItemsSource` ait été peuplé de façon asynchrone (ex. liste de lecteurs chargée après le premier rendu) peut rester avec un texte fermé vide, alors que la sélection est bel et bien prise en compte (`CanStartNow`/logique métier réagissent correctement — confirmé via l'arbre UI Automation, qui montre le bon `SelectedItem` et la liste correcte au clic). Un contournement (ouvrir puis refermer le menu par code, `IsDropDownOpen = true` puis `false` après un court délai) corrige le problème **par intermittence seulement** — pas assez fiable pour être considéré comme une vraie correction, laissé en place en best-effort (`DriveWiperPage`/`DiskAnalyzerPage`). Un simple clic de l'utilisateur sur le menu corrige l'affichage instantanément et durablement ; aucun impact sur le fonctionnement réel (scan, effacement). À revisiter si une vraie solution est trouvée (candidats non testés faute de temps : `SelectedIndex` au lieu de `SelectedItem`, réassigner `ItemsSource` avec une nouvelle collection plutôt que muter l'existante).

**Vérifié à l'exécution** : les 4 pages affichent du contenu réel (pas de placeholder) — Effaceur de disque (lecteurs énumérés, méthodes, avertissement), Doublons (dossier par défaut pré-rempli, bouton Supprimer désactivé tant qu'aucun groupe), Analyseur de disque (`C:\` analysé réellement puis annulé proprement via le bouton Arrêter), Désinstalleur ("Actualiser" liste plus de 200 programmes réels de la machine, y compris des Win32 connus comme Google Chrome/PuTTY/Steam, avatars-initiales affichées, aucune exception dans les logs). `dotnet build`/`dotnet test` verts (64/64). Rien n'est committé à ce stade (comme convenu, jamais sans accord explicite).

**Prochaine étape (à valider avant de commencer)** : tous les items du menu Outils sont maintenant portés — reste soit un vrai `.msix` signé/distribuable (nécessite de statuer sur la signature de code, cf. « Idées restantes »), soit refaire le style des dernières pages non harmonisées (Options, Aide — cf. « État actuel »), soit retirer l'app WPF maintenant que WinUI a parité fonctionnelle sur tous les modules Outils/Nettoyeur/Registre. Détail des phases 0 à 8 originelles : conversation Claude du 2026-09-26/27, à reproduire ici si perdu.

---

## Étape 0 — Cadrage & architecture ✅ Terminé

**Objectif :** poser les fondations du projet avant d'écrire la moindre fonctionnalité.

- Créer une solution .NET 8 `V0XCleaner.sln` avec les projets :
  - `V0XCleaner.App` (WPF, UI, point d'entrée)
  - `V0XCleaner.Core` (logique métier : scanners, nettoyeurs, moteurs, aucune dépendance UI)
  - `V0XCleaner.Services` (accès registre, fichiers, WMI, services Windows, tâches planifiées)
  - `V0XCleaner.Tests` (xUnit)
- Choisir un pattern MVVM (CommunityToolkit.Mvvm recommandé) pour séparer UI et logique.
- Définir la structure de dossiers (Views, ViewModels, Models, Services, Helpers, Resources).
- Mettre en place l'injection de dépendances (Microsoft.Extensions.DependencyInjection).
- Configurer le logging (Serilog) vers `%AppData%\V0XCleaner\logs`.
- Définir le thème visuel (dark/light, style moderne façon Fluent Design / WinUI).
- **Important : prévoir dès le départ un mode "simulation/dry-run"** pour toutes les opérations destructives (aperçu avant suppression réelle).

**Livrable :** solution qui compile, fenêtre principale vide avec menu latéral (Nettoyeur, Registre, Outils, Options, Mises à jour).

---

## Étape 1 — Module Nettoyage système (cœur du "gratuit") ✅ Terminé

**Objectif :** reproduire l'onglet "Cleaner" de CCleaner.

- Scanner et nettoyer :
  - Fichiers temporaires Windows (`%TEMP%`, `Windows\Temp`, cache Windows Update)
  - Corbeille
  - Fichiers journaux (`.log`), rapports d'erreurs Windows (WER)
  - Cache miniatures (thumbnail cache)
  - Fichiers de dump mémoire (`.dmp`)
  - Presse-papiers, historique d'exécution "Exécuter" (Run MRU)
  - Cache DNS (option), cache des icônes
- Nettoyage navigateurs (Chrome, Edge, Firefox, Brave) : cache, cookies (avec exclusions), historique, téléchargements, sessions, données de formulaires — chaque catégorie cochable individuellement.
- Nettoyage applications tierces courantes (Office, Adobe, clients de messagerie) — liste extensible via un système de "définitions" (fichier XML/JSON, comme les `winapp2.ini` de CCleaner).
- UI : arborescence à cases à cocher par catégorie, bouton "Analyser" puis "Exécuter le nettoyage", affichage de l'espace récupérable estimé et récupéré.
- Sécurités : liste noire de chemins protégés, confirmation avant suppression, log détaillé de chaque fichier supprimé (pour audit/annulation manuelle si possible).

**Livrable :** module fonctionnel de bout en bout (scan → aperçu → nettoyage → rapport).

---

## Étape 2 — Nettoyeur de registre ✅ Terminé

**Objectif :** reproduire l'onglet "Registry".

- Scanner les clés orphelines : extensions de fichiers invalides, ActiveX/COM obsolètes, chemins d'application manquants, DLL partagées inexistantes, polices invalides, MUI cache, désinstalleurs fantômes, raccourcis brisés.
- **Toujours proposer une sauvegarde `.reg` automatique avant toute modification** (fonctionnalité critique, non optionnelle).
- UI : liste des problèmes trouvés avec description, case à cocher, bouton "Réparer les problèmes sélectionnés", bouton "Restaurer une sauvegarde".
- Utiliser `Microsoft.Win32.Registry` avec prudence (accès non-admin vs admin, clés HKLM vs HKCU).

**Livrable :** scan registre + sauvegarde + réparation, testé sur clés non critiques d'abord.

---

## Étape 3 — Gestionnaire de démarrage & processus ✅ Terminé

**Objectif :** reproduire "Startup" / "Tools > Startup".

- Lister les programmes au démarrage (Registre Run/RunOnce, dossier Startup, tâches planifiées au logon, services).
- Activer/désactiver/supprimer une entrée, afficher l'éditeur/éditeur, impact estimé sur le temps de boot (si mesurable).
- Extension : gestion des extensions de navigateur au démarrage.

**Livrable :** onglet "Démarrage" complet avec actions activer/désactiver/supprimer.

---

## Étape 4 — Désinstalleur de logiciels ✅ Terminé

**Objectif :** reproduire "Tools > Uninstall".

- Lister les programmes installés (registre `Uninstall`, Windows Apps/UWP via PackageManager).
- Désinstallation simple + option "désinstallation forcée" (suppression des résidus registre/dossiers si le désinstalleur échoue).
- Tri, recherche, export de la liste (CSV).
- Déplacement d'un logiciel (Win32) vers un autre dossier/disque : sauvegarde `.reg`, déplacement du dossier d'installation, jonction laissée à l'ancien emplacement, mise à jour de `InstallLocation`.

**Livrable :** gestionnaire de désinstallation fonctionnel.

---

## Étape 5 — Outils système avancés ✅ Terminé (récupération de fichiers non implémentée, cf. note)

**Objectif :** reproduire les autres sous-outils.

- **Analyseur de disque** (Disk Analyzer) : visualisation de l'espace utilisé par type de fichier/dossier (treemap).
- **Recherche de doublons** (Duplicate Finder) : par nom, taille, hash (MD5/SHA-256).
- **Effaceur de disque** (Drive Wiper) : effacement sécurisé de l'espace libre (1 passe / DoD 3 passes / Gutmann) — **fonctionnalité sensible, bien documenter les risques et demander confirmation forte**.
- **Restauration système** : lister/créer/supprimer des points de restauration (WMI `SystemRestore`).
- **Récupération de fichiers** (File Recovery) : scan basique des secteurs pour fichiers récupérables — fonctionnalité complexe, à considérer en fin de roadmap ou en version simplifiée.

**Livrable :** chaque outil dans un onglet dédié sous "Outils".

---

## Étape 6 — Fonctionnalités "Pro/Professional" (équivalent payant) ✅ Terminé (voir notes de scope)

**Objectif :** couvrir ce qui est habituellement derrière la licence payante.

- **Nettoyage automatique planifié** : intégration Task Scheduler Windows (créer une tâche qui lance `V0XCleaner.exe --silent --clean` à intervalle défini).
- **Surveillance en temps réel** (Active Monitoring) : service en arrière-plan (Windows Service ou tâche + tray icon) qui surveille l'accumulation de fichiers temporaires/junk et notifie l'utilisateur.
- **Mise à jour automatique des définitions et de l'application** (auto-updater, vérification de version via endpoint distant ou GitHub Releases).
- **Health Check / tableau de bord santé système** : score global (sécurité, performance, stabilité, espace disque) avec recommandations priorisées.
- **Nettoyage intelligent en un clic** ("Nettoyage Facile / Easy Clean") automatisé sans intervention.
- **Priorité processus / gestion RAM** : libération de mémoire, ajustement de priorité de processus.
- **Support multi-langue et thèmes personnalisés** (bonus Pro classique).

**Livrable :** module Pro activable, avec icône dans la zone de notification (system tray) pour la surveillance temps réel.

---

## Étape 7 — Sécurité, permissions & robustesse ✅ Terminé

Réalisé : `IElevationService` (détection admin + relance UAC, bouton dans le menu latéral ; manifeste reste asInvoker), drapeaux `RequiresElevatedConfirmation`/`RequiresElevation` (HKLM/HKCR), quarantaine `IQuarantineService` (Undo pour Nettoyeur + Doublons, onglet Outils > Corbeille de sécurité, réglages rétention, purge au démarrage), handlers d'exceptions globaux, 10 nouveaux tests. Le registre garde son Undo via sauvegardes .reg.

- Gérer le manifeste d'élévation UAC (`app.manifest`) — certaines actions nécessitent admin (registre HKLM, services, effacement disque), d'autres non.
- Ajouter un système de restauration/annulation ("Undo") pour les opérations de nettoyage quand c'est possible (quarantaine temporaire avant suppression définitive).
- Tests unitaires sur les modules Core (scanners, calcul d'espace, parsing registre) avec des faux systèmes de fichiers/registre mockés (pas de tests sur le vrai registre système).
- Gestion des exceptions et des accès refusés (fichiers verrouillés, permissions).

---

## Étape 8 — Interface utilisateur & UX finale ✅ Terminé (multi-langue, splash, Mica reportés)

Réalisé : page Accueil (score santé, espace récupérable, dernier nettoyage, quarantaine), icône d'application/fenêtre/tray, Options (exclusions de nettoyage, lancement avec Windows, À propos), version dynamique.

- Design final façon Fluent/WinUI 3 (ou Material via MaterialDesignInXaml si on reste WPF classique), cohérent avec Windows 11 (coins arrondis, Mica/Acrylic si applicable).
- Tableau de bord d'accueil avec résumé (espace récupérable, dernier nettoyage, score santé) — **le Bilan de santé (Étape 6) couvre déjà une bonne partie de ce besoin**, à voir si on le déplace/duplique en page d'accueil.
- Écran "Options" : nettoyage planifié, surveillance, mises à jour, thème **déjà faits (Étape 6)** ; reste à ajouter : exclusions de nettoyage, démarrage de l'app avec Windows, et le support multi-langue (reporté depuis l'Étape 6, gros chantier de traduction).
- Icône, splash screen, nom de l'application "V0X Cleaner" partout (About, installeur, raccourcis).

---

## Étape 9 — Packaging & distribution ✅ Terminé (installeur non compilé : Inno Setup absent)

Réalisé : `installeruild.ps1` (publish self-contained + zip portable + compilation Inno si dispo), `installer\V0XCleaner.iss`, README, CHANGELOG.

- Créer un installeur (Inno Setup ou WiX Toolset) : raccourcis, désinstallation propre, icône, signature de code (si certificat disponible).
- Générer un exécutable self-contained ou dépendant du runtime .NET selon la cible.
- Documentation utilisateur de base (README, changelog).

---

## Notes importantes avant de commencer à coder

1. **Legal/éthique** : reproduire les *fonctionnalités* de CCleaner est légitime, mais il ne faut pas copier son code, ses assets graphiques, son nom/logo, ni ses textes. "V0X Cleaner" doit avoir sa propre identité visuelle.
2. **Risque élevé** : les modules Registre, Effaceur de disque et Nettoyage système touchent directement à l'intégrité du système de l'utilisateur. On avancera avec des sauvegardes systématiques et un mode simulation par défaut.
3. **Ordre recommandé d'implémentation** : Étape 0 → 1 → 3 → 4 → 2 → 5 → 6 → 7 → 8 → 9 (on retarde le Registre après Démarrage/Désinstalleur qui sont moins risqués, pour roder l'architecture d'abord).

---

## Prochaine action

Dire "on commence l'étape 0" (ou toute autre étape) pour que je génère le code correspondant directement dans ce projet.

**État au 20/09/2026 : Étapes 0 à 8 terminées et committées. Feuille de route terminée** (dire "on commence l'étape 9" dans une nouvelle conversation — ce fichier + `git log` suffisent à reprendre le contexte sans relire tout l'historique de conversation).
