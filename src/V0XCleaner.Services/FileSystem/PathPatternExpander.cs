using System.Security;

namespace V0XCleaner.Services.FileSystem;

/// <summary>
/// Résout un motif de chemin contenant des variables d'environnement (%LOCALAPPDATA%...)
/// et des jokers '*' sur un ou plusieurs segments (ex: "%AppData%\Mozilla\Firefox\Profiles\*\cache2")
/// en chemins concrets existant réellement sur le disque. Base du système de "définitions"
/// (à la winapp2.ini) qui pilote la plupart des scanners de nettoyage.
/// </summary>
public static class PathPatternExpander
{
    public static IEnumerable<string> Expand(string pattern)
    {
        var expanded = Environment.ExpandEnvironmentVariables(pattern);
        var segments = expanded.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
        {
            yield break;
        }

        // Premier segment = racine du lecteur (ex: "C:")
        var root = segments[0].EndsWith(':') ? segments[0] + Path.DirectorySeparatorChar : segments[0];

        foreach (var result in ExpandSegments(root, segments.Skip(1).ToArray()))
        {
            yield return result;
        }
    }

    private static IEnumerable<string> ExpandSegments(string currentBase, string[] remaining)
    {
        if (remaining.Length == 0)
        {
            if (File.Exists(currentBase) || Directory.Exists(currentBase))
            {
                yield return currentBase.TrimEnd(Path.DirectorySeparatorChar);
            }

            yield break;
        }

        var segment = remaining[0];
        var rest = remaining.Skip(1).ToArray();

        if (!segment.Contains('*'))
        {
            var combined = Path.Combine(currentBase, segment);
            if (rest.Length == 0)
            {
                if (File.Exists(combined) || Directory.Exists(combined))
                {
                    yield return combined;
                }
            }
            else if (Directory.Exists(combined))
            {
                foreach (var result in ExpandSegments(combined, rest))
                {
                    yield return result;
                }
            }

            yield break;
        }

        if (!Directory.Exists(currentBase))
        {
            yield break;
        }

        if (rest.Length == 0)
        {
            foreach (var file in SafeMatch(() => Directory.GetFiles(currentBase, segment)))
            {
                yield return file;
            }

            foreach (var dir in SafeMatch(() => Directory.GetDirectories(currentBase, segment)))
            {
                yield return dir;
            }
        }
        else
        {
            foreach (var dir in SafeMatch(() => Directory.GetDirectories(currentBase, segment)))
            {
                foreach (var result in ExpandSegments(dir, rest))
                {
                    yield return result;
                }
            }
        }
    }

    private static string[] SafeMatch(Func<string[]> query)
    {
        try
        {
            return query();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or SecurityException)
        {
            return [];
        }
    }
}
