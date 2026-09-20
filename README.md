# V0X Cleaner

Outil de nettoyage et d'optimisation pour Windows 11 (C# / .NET 8 / WPF).

## Fonctionnalités

- **Nettoyeur** : fichiers temporaires, journaux, miniatures, dumps, Corbeille, navigateurs (Chrome, Edge, Brave, Firefox), applications tierces (définitions extensibles dans `Definitions\app-definitions.json`). Mode simulation.
- **Registre** : extensions, COM, DLL partagées, polices, désinstalleurs fantômes, raccourcis brisés — avec sauvegarde `.reg` automatique et restauration.
- **Outils** : bilan de santé, démarrage, désinstalleur, analyseur de disque, doublons, effaceur de disque, restauration système, corbeille de sécurité (Undo).
- **Options** : nettoyage planifié, surveillance en zone de notification, mises à jour GitHub, thème, exclusions, lancement avec Windows.

## Sécurité

Simulation disponible partout, garde-fou de chemins protégés, fichiers nettoyés mis en quarantaine (7 jours par défaut) avant suppression définitive. L'app démarre sans droits administrateur ; le bouton « Relancer en administrateur » du menu latéral donne accès aux actions HKLM.

## Construire

```powershell
dotnet build V0XCleaner.sln
dotnet test V0XCleaner.sln
```

## Distribuer

```powershell
powershell -File installer\build.ps1                    # auto-contenu (aucun runtime requis)
powershell -File installer\build.ps1 -FrameworkDependent # nécessite .NET 8 Desktop Runtime
```

Produit dans `artifacts\` : l'archive portable `.zip` et, si [Inno Setup 6](https://jrsoftware.org/isinfo.php) est installé, l'installeur `V0XCleaner-Setup-<version>.exe` (raccourcis, désinstallation propre). La signature de code n'est pas configurée : ajouter `SignTool` dans `installer\V0XCleaner.iss` si un certificat est disponible.

## Données

Paramètres, journaux, sauvegardes registre et quarantaine : `%AppData%\V0XCleaner`.
