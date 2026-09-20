namespace V0XCleaner.Core.Models;

public sealed record SoftwareUpdate(string Name, string Id, string CurrentVersion, string AvailableVersion);

public sealed record SoftwareUpdateScan(bool WingetAvailable, IReadOnlyList<SoftwareUpdate> Updates, string? Error);
