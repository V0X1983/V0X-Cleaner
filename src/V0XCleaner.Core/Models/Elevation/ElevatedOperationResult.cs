namespace V0XCleaner.Core.Models.Elevation;

/// <summary>Résultat d'une <see cref="ElevatedRegistryOperation"/> exécutée par le helper élevé.</summary>
public sealed record ElevatedOperationResult(string OperationId, bool Success, string? ErrorMessage);
