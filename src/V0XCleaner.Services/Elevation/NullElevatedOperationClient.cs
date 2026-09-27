using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models.Elevation;

namespace V0XCleaner.Services.Elevation;

/// <summary>
/// Implémentation par défaut de <see cref="IElevatedOperationClient"/> : ne lance jamais de helper,
/// échoue toujours proprement. Enregistrée par <c>AddV0XCleanerServices</c> pour que l'app WPF
/// (qui ne câble pas d'implémentation réelle) garde exactement son comportement actuel : les
/// opérations registre nécessitant une élévation échouent silencieusement comme avant, sans invite
/// UAC supplémentaire inattendue.
/// </summary>
public sealed class NullElevatedOperationClient : IElevatedOperationClient
{
    public bool IsSupported => false;

    public Task<ElevatedBatchResponse> ExecuteAsync(
        IReadOnlyList<ElevatedRegistryOperation> operations,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ElevatedBatchResponse(false, "Élévation via helper non disponible dans cette application.", []));
}
