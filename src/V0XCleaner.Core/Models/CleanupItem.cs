namespace V0XCleaner.Core.Models;

/// <summary>
/// Un élément individuel détecté par un scanner (fichier, dossier, clé de registre...)
/// et candidat à la suppression/réparation. Immuable : un nouveau scan produit de nouveaux items.
/// </summary>
public sealed class CleanupItem
{
    public required string Id { get; init; }
    public required string DisplayPath { get; init; }
    public required CleanupCategory Category { get; init; }
    public required long SizeBytes { get; init; }
    public string? Description { get; init; }

    /// <summary>Chemin protégé/sensible nécessitant une confirmation renforcée avant suppression.</summary>
    public bool RequiresElevatedConfirmation { get; init; }
}
