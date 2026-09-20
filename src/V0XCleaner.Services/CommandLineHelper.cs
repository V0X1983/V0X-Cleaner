namespace V0XCleaner.Services;

public static class CommandLineHelper
{
    /// <summary>Extrait le chemin du fichier d'une ligne de commande (guillemets, arguments et variables d'environnement gérés).</summary>
    public static string ExtractFilePath(string command)
    {
        var text = Environment.ExpandEnvironmentVariables(command.Trim());
        if (text.StartsWith('"'))
        {
            var end = text.IndexOf('"', 1);
            return end > 1 ? text[1..end] : text.Trim('"');
        }

        var exe = text.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        return exe >= 0 ? text[..(exe + 4)] : text;
    }
}
