namespace V0XCleaner.Core.Models;

/// <summary>Résultat générique d'une opération ponctuelle (création/suppression d'un point de restauration...).</summary>
public sealed record OperationOutcome(bool Success, string Message);
