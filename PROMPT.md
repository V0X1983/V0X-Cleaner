# V0X Cleaner — Feuille de route de développement

Application Windows 11 de nettoyage et d'optimisation système, équivalente à CCleaner (version gratuite + fonctionnalités Pro/Professional), développée en **C# / .NET 8 / WPF**.

> Ce document sert de prompt directeur étape par étape. Chaque étape peut être donnée à Claude Code (ou suivie manuellement) indépendamment, dans l'ordre. Cocher au fur et à mesure.

---

## Étape 0 — Cadrage & architecture

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

## Étape 1 — Module Nettoyage système (cœur du "gratuit")

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

## Étape 2 — Nettoyeur de registre

**Objectif :** reproduire l'onglet "Registry".

- Scanner les clés orphelines : extensions de fichiers invalides, ActiveX/COM obsolètes, chemins d'application manquants, DLL partagées inexistantes, polices invalides, MUI cache, désinstalleurs fantômes, raccourcis brisés.
- **Toujours proposer une sauvegarde `.reg` automatique avant toute modification** (fonctionnalité critique, non optionnelle).
- UI : liste des problèmes trouvés avec description, case à cocher, bouton "Réparer les problèmes sélectionnés", bouton "Restaurer une sauvegarde".
- Utiliser `Microsoft.Win32.Registry` avec prudence (accès non-admin vs admin, clés HKLM vs HKCU).

**Livrable :** scan registre + sauvegarde + réparation, testé sur clés non critiques d'abord.

---

## Étape 3 — Gestionnaire de démarrage & processus

**Objectif :** reproduire "Startup" / "Tools > Startup".

- Lister les programmes au démarrage (Registre Run/RunOnce, dossier Startup, tâches planifiées au logon, services).
- Activer/désactiver/supprimer une entrée, afficher l'éditeur/éditeur, impact estimé sur le temps de boot (si mesurable).
- Extension : gestion des extensions de navigateur au démarrage.

**Livrable :** onglet "Démarrage" complet avec actions activer/désactiver/supprimer.

---

## Étape 4 — Désinstalleur de logiciels

**Objectif :** reproduire "Tools > Uninstall".

- Lister les programmes installés (registre `Uninstall`, Windows Apps/UWP via PackageManager).
- Désinstallation simple + option "désinstallation forcée" (suppression des résidus registre/dossiers si le désinstalleur échoue).
- Tri, recherche, export de la liste (CSV).

**Livrable :** gestionnaire de désinstallation fonctionnel.

---

## Étape 5 — Outils système avancés

**Objectif :** reproduire les autres sous-outils.

- **Analyseur de disque** (Disk Analyzer) : visualisation de l'espace utilisé par type de fichier/dossier (treemap).
- **Recherche de doublons** (Duplicate Finder) : par nom, taille, hash (MD5/SHA-256).
- **Effaceur de disque** (Drive Wiper) : effacement sécurisé de l'espace libre (1 passe / DoD 3 passes / Gutmann) — **fonctionnalité sensible, bien documenter les risques et demander confirmation forte**.
- **Restauration système** : lister/créer/supprimer des points de restauration (WMI `SystemRestore`).
- **Récupération de fichiers** (File Recovery) : scan basique des secteurs pour fichiers récupérables — fonctionnalité complexe, à considérer en fin de roadmap ou en version simplifiée.

**Livrable :** chaque outil dans un onglet dédié sous "Outils".

---

## Étape 6 — Fonctionnalités "Pro/Professional" (équivalent payant)

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

## Étape 7 — Sécurité, permissions & robustesse

- Gérer le manifeste d'élévation UAC (`app.manifest`) — certaines actions nécessitent admin (registre HKLM, services, effacement disque), d'autres non.
- Ajouter un système de restauration/annulation ("Undo") pour les opérations de nettoyage quand c'est possible (quarantaine temporaire avant suppression définitive).
- Tests unitaires sur les modules Core (scanners, calcul d'espace, parsing registre) avec des faux systèmes de fichiers/registre mockés (pas de tests sur le vrai registre système).
- Gestion des exceptions et des accès refusés (fichiers verrouillés, permissions).

---

## Étape 8 — Interface utilisateur & UX finale

- Design final façon Fluent/WinUI 3 (ou Material via MaterialDesignInXaml si on reste WPF classique), cohérent avec Windows 11 (coins arrondis, Mica/Acrylic si applicable).
- Tableau de bord d'accueil avec résumé (espace récupérable, dernier nettoyage, score santé).
- Écran "Options" complet (exclusions, langue, démarrage avec Windows, thème, planification).
- Icône, splash screen, nom de l'application "V0X Cleaner" partout (About, installeur, raccourcis).

---

## Étape 9 — Packaging & distribution

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
