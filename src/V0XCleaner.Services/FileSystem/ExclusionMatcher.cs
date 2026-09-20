namespace V0XCleaner.Services.FileSystem;

/// <summary>Détermine si un chemin est couvert par la liste d'exclusions de l'utilisateur (fichier exact ou dossier parent).</summary>
public static class ExclusionMatcher
{
    public static bool IsExcluded(string path, IEnumerable<string> exclusions)
    {
        string full;
        try
        {
            full = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }

        foreach (var exclusion in exclusions)
        {
            if (string.IsNullOrWhiteSpace(exclusion))
            {
                continue;
            }

            string ex;
            try
            {
                ex = Path.GetFullPath(exclusion.Trim()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException)
            {
                continue;
            }

            if (full.Equals(ex, StringComparison.OrdinalIgnoreCase) ||
                full.StartsWith(ex + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
