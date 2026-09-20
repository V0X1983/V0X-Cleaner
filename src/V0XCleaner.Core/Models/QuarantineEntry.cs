namespace V0XCleaner.Core.Models;

/// <summary>
/// Un fichier déplacé vers la "corbeille de sécurité" de V0X Cleaner au lieu d'être supprimé
/// définitivement (Étape 7 — Undo). Reste récupérable jusqu'à restauration manuelle ou purge
/// automatique après expiration du délai de rétention configuré.
/// </summary>
public sealed class QuarantineEntry
{
    public required string Id { get; init; }
    public required string OriginalPath { get; init; }
    public required string QuarantinedPath { get; init; }
    public required long SizeBytes { get; init; }
    public required DateTime QuarantinedAtUtc { get; init; }
}
