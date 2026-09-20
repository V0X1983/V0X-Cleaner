namespace V0XCleaner.Core.Models;

/// <summary>Un programme classique (registre Uninstall) ou une application du Microsoft Store (paquet UWP/AppX).</summary>
public sealed class InstalledProgram
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public string? DisplayVersion { get; init; }
    public string? Publisher { get; init; }
    public long? EstimatedSizeBytes { get; init; }
    public DateTime? InstallDate { get; init; }
    public required InstalledProgramKind Kind { get; init; }

    /// <summary>Commande de désinstallation "normale" (UninstallString). Null pour les paquets UWP.</summary>
    public string? UninstallCommand { get; init; }

    /// <summary>Commande silencieuse (QuietUninstallString), si le programme en fournit une.</summary>
    public string? QuietUninstallCommand { get; init; }

    public string? InstallLocation { get; init; }

    /// <summary>Fichier (exe, dll ou ico) dont on peut extraire l'icône du programme, si connu.</summary>
    public string? IconPath { get; init; }

    /// <summary>False pour les paquets système protégés (ex: NonRemovable côté UWP).</summary>
    public bool CanUninstall { get; init; } = true;
}
