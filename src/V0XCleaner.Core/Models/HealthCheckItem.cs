namespace V0XCleaner.Core.Models;

public sealed class HealthCheckItem
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required int Score { get; init; }
    public required HealthCheckSeverity Severity { get; init; }
    public required string Recommendation { get; init; }
}
