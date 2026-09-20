namespace V0XCleaner.Core.Models;

public sealed record RunningProcessInfo(int Id, string Name, long MemoryBytes, bool HasWindow);
