using V0XCleaner.Core.Models;

namespace V0XCleaner.Core.Abstractions;

/// <summary>Interroge la dernière publication GitHub Releases d'un dépôt configuré par l'utilisateur.</summary>
public interface IUpdateChecker
{
    Task<LatestReleaseInfo> GetLatestReleaseAsync(string owner, string repo, CancellationToken cancellationToken = default);
}
