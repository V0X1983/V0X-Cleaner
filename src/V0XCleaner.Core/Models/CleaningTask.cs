using V0XCleaner.Core.Abstractions;

namespace V0XCleaner.Core.Models;

/// <summary>
/// Association explicite entre un scanner et le nettoyeur capable de traiter ses résultats,
/// enrichie des métadonnées d'affichage. C'est l'unité que consomme directement l'UI du
/// Nettoyeur (une ligne cochable dans l'arborescence).
/// </summary>
public sealed class CleaningTask
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required CleaningSection Section { get; init; }
    public required CleanupCategory Category { get; init; }
    public required IScanner Scanner { get; init; }
    public required ICleaner Cleaner { get; init; }
    public bool SelectedByDefault { get; init; } = true;
}
