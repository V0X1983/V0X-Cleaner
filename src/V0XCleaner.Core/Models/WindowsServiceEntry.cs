namespace V0XCleaner.Core.Models;

public enum ServiceStartMode
{
    Automatic,
    Manual,
    Disabled
}

/// <summary>Un service Windows non Microsoft (les services du système ne sont volontairement pas proposés).</summary>
public sealed class WindowsServiceEntry
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required string FilePath { get; init; }
    public required string Publisher { get; init; }
    public required ServiceStartMode StartMode { get; init; }
    public required bool IsRunning { get; init; }
}
