# Changelog

## Non publié

- Désinstalleur : tri par clic sur les colonnes Programme/Taille (bascule du sens, en plus du menu de tri existant).
- Mise à jour de logiciels : les mises à jour qui exigent un redémarrage (ex. pilotes, gros runtimes) ne s'affichent plus à tort comme échouées.
- Nettoyeur et Registre : l'écran de chargement affiche la catégorie en cours et son nombre d'éléments pendant un gros nettoyage.
- Vérification automatique (silencieuse) d'une nouvelle version au démarrage : un lien discret apparaît dans le menu latéral si une version plus récente existe (ouvre la page de la release ; le téléchargement et l'installation restent une action manuelle depuis Options).

## 0.2.0

- Nouvelles pages : Mise à jour de logiciels (winget), Mise à jour de pilotes (liste + Windows Update), Optimiseur de performances, Aide.
- Section Stockage cloud dans le Nettoyeur (journaux et caches OneDrive, Google Drive, Dropbox).
- Thème appliqué à tous les contrôles.
- Le Bilan de santé remplace la page Accueil : assistant en trois étapes (confidentialité, espace, problèmes).
- Nouveau style unifié (onglets, colonnes, interrupteurs, barre d'actions) : Mise à jour de logiciels, Optimiseur, Démarrage, Nettoyeur, Registre, Désinstalleur.
- Optimiseur : mise en veille des applications (onglets actives / en veille / exclues).
- Démarrage : onglets Menu contextuel et Services Windows.
- Mise à jour de logiciels : progression du téléchargement et de l'installation, plus de blocage de winget, protection contre les redémarrages imposés par les installateurs.
- Registre : seules les 10 dernières sauvegardes .reg sont conservées.
- Vérification des mises à jour sur le dépôt officiel V0X1983/V0X-Cleaner par défaut.
- Mise à jour automatique : « Vérifier maintenant » télécharge la nouvelle version (empreinte SHA-256 vérifiée), l'installe en silencieux puis relance l'application.
- Désinstalleur : déplacement d'un logiciel vers un autre dossier ou disque (jonction laissée à l'ancien emplacement, sauvegarde .reg).
- Menu latéral : message de soutien avec adresse pour un virement.

## 0.1.0

- Nettoyeur système, navigateurs et applications tierces (simulation, exclusions).
- Nettoyeur de registre avec sauvegarde/restauration `.reg`.
- Outils : démarrage, désinstalleur, analyseur de disque, doublons, effaceur, restauration système, bilan de santé.
- Corbeille de sécurité (quarantaine + restauration), élévation UAC à la demande.
- Nettoyage planifié, surveillance en temps réel, vérification des mises à jour.
- Page Accueil, thèmes clair/sombre, packaging (archive portable + script Inno Setup).
