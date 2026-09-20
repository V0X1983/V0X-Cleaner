namespace V0XCleaner.Core.Models;

/// <summary>Un groupe de fichiers strictement identiques (même taille, même hash SHA-256).</summary>
public sealed class DuplicateFileGroup
{
    public required long SizeBytes { get; init; }
    public required IReadOnlyList<string> FilePaths { get; init; }

    /// <summary>Espace récupérable si l'on ne garde qu'un seul exemplaire du groupe.</summary>
    public long WastedBytes => SizeBytes * (FilePaths.Count - 1);
}
