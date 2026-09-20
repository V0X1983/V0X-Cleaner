namespace V0XCleaner.Core.Models;

/// <summary>Un pilote proposé par Windows Update (signé Microsoft) et non encore installé.</summary>
public sealed record DriverUpdateInfo(string Id, string Title, string DeviceClass, string Version, DateTime? Published);
