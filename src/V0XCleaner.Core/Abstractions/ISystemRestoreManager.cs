using V0XCleaner.Core.Models;

namespace V0XCleaner.Core.Abstractions;

public interface ISystemRestoreManager
{
    Task<IReadOnlyList<RestorePointInfo>> GetRestorePointsAsync(CancellationToken cancellationToken = default);

    Task<OperationOutcome> CreateRestorePointAsync(string description, CancellationToken cancellationToken = default);

    Task<OperationOutcome> DeleteRestorePointAsync(int sequenceNumber, CancellationToken cancellationToken = default);
}
