using V0XCleaner.Core.Abstractions;

namespace V0XCleaner.Services.FileSystem;

/// <summary>
/// Implémentation par défaut de <see cref="IPathGuard"/>. Bloque uniquement les racines
/// sensibles elles-mêmes (Windows, Program Files, racine du profil utilisateur, racine de
/// lecteur...), pas leur contenu : un scanner peut légitimement vouloir nettoyer l'intérieur
/// de "C:\Windows\Temp", mais jamais supprimer "C:\Windows" en bloc.
/// </summary>
public sealed class PathGuard : IPathGuard
{
    private readonly HashSet<string> _protectedExactPaths;

    public PathGuard()
    {
        _protectedExactPaths = BuildProtectedPaths();
    }

    public bool IsSafeToDelete(string fullPath, out string? reason)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            reason = "Chemin vide.";
            return false;
        }

        string normalized;
        try
        {
            normalized = Normalize(Path.GetFullPath(fullPath));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            reason = $"Chemin invalide : {ex.Message}";
            return false;
        }

        var root = Normalize(Path.GetPathRoot(normalized) ?? string.Empty);
        if (!string.IsNullOrEmpty(root) && string.Equals(normalized, root, StringComparison.OrdinalIgnoreCase))
        {
            reason = "Impossible de supprimer une racine de lecteur.";
            return false;
        }

        if (_protectedExactPaths.Contains(normalized))
        {
            reason = "Ce chemin fait partie des dossiers système protégés.";
            return false;
        }

        reason = null;
        return true;
    }

    private static string Normalize(string path) =>
        path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private static HashSet<string> BuildProtectedPaths()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(Environment.SpecialFolder folder)
        {
            var value = Environment.GetFolderPath(folder);
            if (!string.IsNullOrWhiteSpace(value))
            {
                paths.Add(Normalize(value));
            }
        }

        Add(Environment.SpecialFolder.Windows);
        Add(Environment.SpecialFolder.System);
        Add(Environment.SpecialFolder.SystemX86);
        Add(Environment.SpecialFolder.ProgramFiles);
        Add(Environment.SpecialFolder.ProgramFilesX86);
        Add(Environment.SpecialFolder.CommonProgramFiles);
        Add(Environment.SpecialFolder.CommonProgramFilesX86);
        Add(Environment.SpecialFolder.UserProfile);
        Add(Environment.SpecialFolder.CommonApplicationData); // ProgramData

        // Racine "C:\Users" : parent du profil utilisateur, ne doit jamais être ciblée.
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var usersRoot = Path.GetDirectoryName(userProfile);
        if (!string.IsNullOrWhiteSpace(usersRoot))
        {
            paths.Add(Normalize(usersRoot));
        }

        // L'application elle-même ne doit jamais se supprimer.
        paths.Add(Normalize(AppContext.BaseDirectory));

        return paths;
    }
}
