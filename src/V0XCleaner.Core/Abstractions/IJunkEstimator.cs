namespace V0XCleaner.Core.Abstractions;

/// <summary>Estime rapidement l'espace récupérable (catégories système par défaut uniquement), pour la surveillance en arrière-plan.</summary>
public interface IJunkEstimator
{
    Task<long> EstimateReclaimableBytesAsync(CancellationToken cancellationToken = default);
}
