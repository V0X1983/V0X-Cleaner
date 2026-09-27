using V0XCleaner.Core.Models.Elevation;

namespace V0XCleaner.Core.Abstractions;

/// <summary>
/// Exécute un lot d'opérations registre nécessitant les droits administrateur en les déléguant à
/// V0XCleaner.ElevatedHelper (process compagnon élevé, non empaqueté et à chemin fixe) plutôt
/// qu'en élevant tout le process courant — nécessaire pour l'app WinUI 3/MSIX, où le process
/// principal reste toujours asInvoker (voir PROMPT.md, section "Migration en cours").
///
/// Toutes les opérations d'un même appel sont exécutées par UNE SEULE invite UAC (le lot entier
/// est passé au helper en une fois) : les appelants doivent regrouper leurs opérations plutôt que
/// d'appeler cette méthode item par item.
///
/// Implémentation par défaut enregistrée dans <c>AddV0XCleanerServices</c> :
/// <c>NullElevatedOperationClient</c> (IsSupported = false, ne lance jamais de process) — l'app
/// WPF ne câble pas d'implémentation réelle et garde donc son comportement actuel inchangé.
/// </summary>
public interface IElevatedOperationClient
{
    /// <summary>True si cette application a câblé une implémentation réelle du helper (indépendant du succès de l'élévation elle-même).</summary>
    bool IsSupported { get; }

    /// <summary>
    /// Exécute <paramref name="operations"/> (peut être vide, pour simplement vérifier que
    /// l'élévation fonctionne) via une unique invocation élevée de V0XCleaner.ElevatedHelper.
    /// </summary>
    Task<ElevatedBatchResponse> ExecuteAsync(
        IReadOnlyList<ElevatedRegistryOperation> operations,
        CancellationToken cancellationToken = default);
}
