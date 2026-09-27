using V0XCleaner.Services.Native;

namespace V0XCleaner.Services.Elevation;

/// <summary>
/// Localise et déploie une copie de V0XCleaner.ElevatedHelper.exe à un chemin fixe
/// (<see cref="AppPaths.HelperFolder"/>), pour que ce chemin reste stable même si l'app
/// principale est packagée MSIX (dont l'emplacement change à chaque mise à jour).
///
/// LIMITE CONNUE (Phase 2) : la recherche du helper source se fait en remontant jusqu'au dossier
/// contenant V0XCleaner.sln puis en cherchant sous src/V0XCleaner.ElevatedHelper/bin — cela ne
/// fonctionne qu'en environnement de développement (dotnet run/build depuis le dépôt). La Phase 7
/// (packaging) devra remplacer ceci par un vrai mécanisme d'empaquetage du helper avec
/// l'installeur/le paquet MSIX.
/// </summary>
public sealed class ElevatedHelperLauncher
{
    private const string HelperExeName = "V0XCleaner.ElevatedHelper.exe";

    public string? DeployedHelperExePath { get; private set; }

    public bool TryEnsureDeployed()
    {
        var source = ResolveSourceHelperExePath();
        if (source is null)
        {
            return false;
        }

        var destination = Path.Combine(AppPaths.HelperFolder, HelperExeName);
        var sourceDir = Path.GetDirectoryName(source)!;

        if (!File.Exists(destination) || File.GetLastWriteTimeUtc(source) > File.GetLastWriteTimeUtc(destination))
        {
            foreach (var file in Directory.EnumerateFiles(sourceDir))
            {
                var targetPath = Path.Combine(AppPaths.HelperFolder, Path.GetFileName(file));
                File.Copy(file, targetPath, overwrite: true);
            }
        }

        DeployedHelperExePath = destination;
        return File.Exists(destination);
    }

    private static string? ResolveSourceHelperExePath()
    {
        var solutionRoot = FindSolutionRoot(AppContext.BaseDirectory);
        if (solutionRoot is null)
        {
            return null;
        }

        var helperBin = Path.Combine(solutionRoot, "src", "V0XCleaner.ElevatedHelper", "bin");
        if (!Directory.Exists(helperBin))
        {
            return null;
        }

        return Directory.EnumerateFiles(helperBin, HelperExeName, SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static string? FindSolutionRoot(string startDirectory)
    {
        var dir = new DirectoryInfo(startDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("V0XCleaner.sln").Length > 0)
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
