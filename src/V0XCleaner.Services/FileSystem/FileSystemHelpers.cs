using System.Security;

namespace V0XCleaner.Services.FileSystem;

/// <summary>
/// Opérations fichier/dossier tolérantes aux erreurs (accès refusé, fichier verrouillé,
/// chemin trop long...). Un scanner ou un nettoyeur ne doit jamais planter à cause d'un
/// seul fichier problématique : on l'ignore et on continue.
/// </summary>
public static class FileSystemHelpers
{
    /// <summary>Énumère récursivement les fichiers d'un dossier, en ignorant les sous-dossiers inaccessibles.</summary>
    public static IEnumerable<FileInfo> EnumerateFilesSafe(string rootDirectory)
    {
        if (!Directory.Exists(rootDirectory))
        {
            yield break;
        }

        var pending = new Stack<string>();
        pending.Push(rootDirectory);

        while (pending.Count > 0)
        {
            var current = pending.Pop();

            IEnumerable<string> subDirectories = [];
            IEnumerable<string> files = [];

            try
            {
                subDirectories = Directory.EnumerateDirectories(current).ToList();
                files = Directory.EnumerateFiles(current).ToList();
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or SecurityException)
            {
                continue;
            }

            foreach (var dir in subDirectories)
            {
                pending.Push(dir);
            }

            foreach (var file in files)
            {
                FileInfo? info = null;
                try
                {
                    info = new FileInfo(file);
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or SecurityException)
                {
                    // ignoré
                }

                if (info is not null)
                {
                    yield return info;
                }
            }
        }
    }

    /// <summary>Taille totale d'un dossier, en ignorant silencieusement les fichiers inaccessibles.</summary>
    public static long GetDirectorySizeSafe(string directory)
    {
        long total = 0;
        foreach (var file in EnumerateFilesSafe(directory))
        {
            try
            {
                total += file.Length;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                // ignoré
            }
        }

        return total;
    }

    /// <summary>Taille d'un fichier unique, 0 si inaccessible.</summary>
    public static long GetFileSizeSafe(string path)
    {
        try
        {
            return new FileInfo(path).Length;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return 0;
        }
    }

    /// <summary>Supprime un fichier. Retourne false + message d'erreur en cas d'échec (verrouillé, permissions...).</summary>
    public static bool TryDeleteFile(string path, out string? error)
    {
        try
        {
            if (File.Exists(path))
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
            }

            error = null;
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or SecurityException)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Supprime récursivement le CONTENU d'un dossier (fichiers + sous-dossiers) sans supprimer
    /// le dossier racine lui-même. Continue même si certains éléments échouent, et rapporte
    /// chaque échec individuellement.
    /// </summary>
    public static IReadOnlyList<(string Path, string Error)> DeleteDirectoryContents(string directory)
    {
        var errors = new List<(string, string)>();

        if (!Directory.Exists(directory))
        {
            return errors;
        }

        IEnumerable<string> files;
        IEnumerable<string> subDirectories;
        try
        {
            files = Directory.EnumerateFiles(directory).ToList();
            subDirectories = Directory.EnumerateDirectories(directory).ToList();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            errors.Add((directory, ex.Message));
            return errors;
        }

        foreach (var file in files)
        {
            if (!TryDeleteFile(file, out var error))
            {
                errors.Add((file, error!));
            }
        }

        foreach (var subDirectory in subDirectories)
        {
            errors.AddRange(DeleteDirectoryContents(subDirectory));

            try
            {
                if (!Directory.EnumerateFileSystemEntries(subDirectory).Any())
                {
                    Directory.Delete(subDirectory, recursive: false);
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                errors.Add((subDirectory, ex.Message));
            }
        }

        return errors;
    }
}
