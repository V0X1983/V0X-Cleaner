namespace V0XCleaner.Core.Models;

public sealed class HealthCheckResult
{
    public required int OverallScore { get; init; }
    public required IReadOnlyList<HealthCheckItem> Items { get; init; }
}
