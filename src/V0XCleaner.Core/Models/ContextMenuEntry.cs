namespace V0XCleaner.Core.Models;

/// <summary>Où l'entrée apparaît : clic droit sur un dossier, sur le fond d'un dossier ou du bureau, ou sur un fichier.</summary>
public enum ContextMenuScope
{
    Directory,
    Background,
    File
}

/// <summary>Verbe (commande « shell ») ou extension COM (« ContextMenuHandlers »), désactivés de deux façons différentes.</summary>
public enum ContextMenuEntryKind
{
    Verb,
    Handler
}

/// <summary>Une entrée du menu contextuel de l'Explorateur ajoutée par un logiciel tiers.</summary>
public sealed class ContextMenuEntry
{
    public required string Name { get; init; }
    public required string FilePath { get; init; }
    public required string Publisher { get; init; }
    public required ContextMenuScope Scope { get; init; }
    public required ContextMenuEntryKind Kind { get; init; }

    /// <summary>Chemin de la clé sous HKEY_CLASSES_ROOT (verbe) ou de la clé du gestionnaire.</summary>
    public required string RegistryPath { get; init; }

    /// <summary>CLSID de l'extension pour un gestionnaire ; null pour un verbe.</summary>
    public string? Clsid { get; init; }

    public required bool IsEnabled { get; init; }
}
