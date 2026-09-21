using V0XCleaner.Core.Models;

namespace V0XCleaner.Core.Abstractions;

/// <summary>Liste et pilote la désinstallation des programmes classiques et des applications du Microsoft Store.</summary>
public interface IInstalledProgramsCatalog
{
    Task<IReadOnlyList<InstalledProgram>> GetProgramsAsync(CancellationToken cancellationToken = default);

    /// <summary>Lance le désinstalleur officiel du programme (silencieux si demandé et disponible).</summary>
    Task<UninstallOutcome> UninstallAsync(InstalledProgram program, bool preferSilent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Supprime directement le dossier d'installation et l'entrée de registre, sans lancer le
    /// désinstalleur : à utiliser seulement quand celui-ci a échoué ou n'existe plus.
    /// Ne s'applique qu'aux programmes Win32 (registre).
    /// </summary>
    Task<UninstallOutcome> ForceRemoveResidueAsync(InstalledProgram program, CancellationToken cancellationToken = default);

    /// <summary>
    /// Déplace le dossier d'installation vers <paramref name="destinationParentFolder"/> (ex: un autre
    /// disque), laisse une jonction à l'ancien emplacement pour que le programme continue de
    /// fonctionner, et met à jour InstallLocation dans le registre (sauvegarde .reg préalable).
    /// Ne s'applique qu'aux programmes Win32 dont le dossier d'installation est connu.
    /// </summary>
    Task<UninstallOutcome> MoveAsync(InstalledProgram program, string destinationParentFolder, CancellationToken cancellationToken = default);
}
