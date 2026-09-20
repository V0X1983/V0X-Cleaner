namespace V0XCleaner.Core.Models;

public sealed record LatestReleaseInfo(bool Success, string? Version, string? ReleaseUrl, string Message);
