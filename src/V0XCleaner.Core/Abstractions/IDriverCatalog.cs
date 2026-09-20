using V0XCleaner.Core.Models;

namespace V0XCleaner.Core.Abstractions;

/// <summary>Liste les pilotes installés (lecture seule : V0X Cleaner n'installe jamais de pilote lui-même).</summary>
public interface IDriverCatalog
{
    Task<IReadOnlyList<DriverInfo>> GetDriversAsync(CancellationToken cancellationToken = default);
}
