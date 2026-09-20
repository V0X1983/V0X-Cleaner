namespace V0XCleaner.Services.Native;

/// <summary>Emplacements disque standard de l'application, centralisés pour éviter les chemins en dur.</summary>
public static class AppPaths
{
    public static string RootFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "V0XCleaner");

    public static string LogsFolder => EnsureExists(Path.Combine(RootFolder, "logs"));

    public static string RegistryBackupsFolder => EnsureExists(Path.Combine(RootFolder, "registry-backups"));

    public static string QuarantineFolder => EnsureExists(Path.Combine(RootFolder, "quarantine"));

    public static string ConfigFilePath => Path.Combine(EnsureExists(RootFolder), "config.json");

    private static string EnsureExists(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
