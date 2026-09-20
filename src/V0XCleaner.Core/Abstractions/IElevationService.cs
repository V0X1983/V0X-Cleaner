namespace V0XCleaner.Core.Abstractions;

/// <summary>
/// Détecte si le processus courant a été lancé avec les droits administrateur et permet de
/// relancer l'application élevée à la demande. V0X Cleaner démarre par défaut en asInvoker
/// (voir app.manifest) : l'élévation est ciblée sur les actions qui en ont besoin (registre HKLM,
/// services, démarrage système) plutôt que forcée pour toute l'application.
/// </summary>
public interface IElevationService
{
    /// <summary>True si le processus courant s'exécute avec les droits administrateur.</summary>
    bool IsElevated { get; }

    /// <summary>
    /// Relance l'application avec élévation UAC (déclenche l'invite Windows). Retourne false si
    /// l'utilisateur refuse l'invite ou si le lancement échoue.
    /// </summary>
    bool RelaunchElevated();
}
