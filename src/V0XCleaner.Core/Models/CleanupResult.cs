namespace V0XCleaner.Core.Models;

/// <summary>Résultat de l'exécution (ou simulation) d'un nettoyage sur un ensemble de CleanupItem.</summary>
public sealed class CleanupResult
{
    public required OperationMode Mode { get; init; }
    public required int SucceededCount { get; init; }
    public required int FailedCount { get; init; }
    public required long FreedBytes { get; init; }
    public required IReadOnlyList<CleanupError> Errors { get; init; }
}

public sealed record CleanupError(string ItemId, string Message);
