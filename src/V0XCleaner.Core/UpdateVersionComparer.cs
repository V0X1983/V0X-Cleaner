namespace V0XCleaner.Core;

/// <summary>Compare la version annoncée par une release GitHub à la version installée de l'assembly.</summary>
public static class UpdateVersionComparer
{
    public static bool IsNewer(string? latestVersion, Version? installedVersion)
    {
        if (installedVersion is null || !Version.TryParse(latestVersion, out var latest))
        {
            return false;
        }

        var installed = new Version(installedVersion.Major, installedVersion.Minor, Math.Max(installedVersion.Build, 0));
        return latest > installed;
    }
}
