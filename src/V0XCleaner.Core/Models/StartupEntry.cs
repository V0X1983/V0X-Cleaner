namespace V0XCleaner.Core.Models;

/// <summary>
/// Un programme, raccourci ou tâche planifiée qui se lance à la connexion. Contrairement au
/// Nettoyeur/Registre (scan puis suppression en masse), chaque entrée s'active/se désactive
/// individuellement, comme dans le Gestionnaire des tâches de Windows.
/// </summary>
public sealed class StartupEntry
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Command { get; init; }
    public required StartupEntrySource Source { get; init; }
    public required bool IsEnabled { get; init; }

    /// <summary>Emplacement d'origine (clé de registre, dossier, chemin de tâche planifiée) pour affichage.</summary>
    public required string Location { get; init; }

    /// <summary>True si la modification de cette entrée nécessite les droits administrateur (HKLM, dossier de démarrage commun...).</summary>
    public bool RequiresElevation { get; init; }
}
