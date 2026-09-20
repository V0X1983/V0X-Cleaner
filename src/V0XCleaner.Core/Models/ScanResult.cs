namespace V0XCleaner.Core.Models;

/// <summary>Résultat d'un scan : l'ensemble des éléments trouvés par un scanner donné.</summary>
public sealed class ScanResult
{
    public required IReadOnlyList<CleanupItem> Items { get; init; }
    public long TotalSizeBytes => Items.Sum(i => i.SizeBytes);
}
