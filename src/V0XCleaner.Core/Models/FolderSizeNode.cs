namespace V0XCleaner.Core.Models;

/// <summary>Un fichier ou dossier avec sa taille totale, pour l'affichage dans l'Analyseur de disque.</summary>
public sealed class FolderSizeNode
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public required long SizeBytes { get; init; }
    public required int FileCount { get; init; }
    public required bool IsFile { get; init; }
}
