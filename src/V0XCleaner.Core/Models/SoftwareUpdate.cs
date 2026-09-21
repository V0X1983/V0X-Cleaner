namespace V0XCleaner.Core.Models;

public sealed record SoftwareUpdate(string Name, string Id, string CurrentVersion, string AvailableVersion);

public sealed record SoftwareUpdateScan(bool WingetAvailable, IReadOnlyList<SoftwareUpdate> Updates, string? Error);

/// <summary>Résultat d'une mise à jour ; <see cref="Message"/> explique l'échec quand la cause est connue.</summary>
public sealed record SoftwareUpdateResult(bool Success, string? Message = null);

public enum SoftwareUpdatePhase
{
    Downloading,
    Installing
}

/// <summary>Avancement d'une mise à jour ; <see cref="Percent"/> est nul quand il est inconnu.</summary>
public sealed record SoftwareUpdateProgress(SoftwareUpdatePhase Phase, double? Percent);
