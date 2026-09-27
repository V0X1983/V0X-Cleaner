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

    /// <summary>
    /// Copie locale à chemin fixe de V0XCleaner.ElevatedHelper.exe (voir ElevatedHelperLauncher).
    /// Chemin stable indépendant de l'emplacement du paquet MSIX de l'app principale, qui change à
    /// chaque mise à jour.
    /// </summary>
    public static string HelperFolder => EnsureExists(Path.Combine(RootFolder, "helper"));

    public static string ElevatedTempFolder => EnsureExists(Path.Combine(RootFolder, "elevated-temp"));

    private static string EnsureExists(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
