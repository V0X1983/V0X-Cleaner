namespace V0XCleaner.Core.Models;

public sealed class RestorePointInfo
{
    public required int SequenceNumber { get; init; }
    public required string Description { get; init; }
    public required DateTime CreationTime { get; init; }
    public required int RestorePointType { get; init; }
}
