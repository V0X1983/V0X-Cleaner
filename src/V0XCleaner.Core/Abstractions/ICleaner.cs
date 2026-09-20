using V0XCleaner.Core.Models;

namespace V0XCleaner.Core.Abstractions;

/// <summary>
/// Applique (ou simule, selon <see cref="OperationMode"/>) la suppression/réparation
/// d'une liste de <see cref="CleanupItem"/> précédemment produite par un <see cref="IScanner"/>.
/// </summary>
public interface ICleaner
{
    /// <summary>Doit correspondre au Key du IScanner qui a produit les items acceptés.</summary>
    string Key { get; }

    Task<CleanupResult> CleanAsync(
        IReadOnlyList<CleanupItem> items,
        OperationMode mode,
        CancellationToken cancellationToken = default);
}
