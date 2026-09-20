using System.Text.Json;
using V0XCleaner.Services.Native;

namespace V0XCleaner.Services.Definitions;

/// <summary>
/// Charge le catalogue de définitions d'applications tierces : le fichier intégré à l'application,
/// puis tout fichier JSON ajouté par l'utilisateur dans %AppData%\V0XCleaner\definitions
/// (mêmes clés = l'utilisateur peut surcharger une définition intégrée).
/// </summary>
public static class AppDefinitionsLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<AppDefinition> LoadAll()
    {
        var result = new Dictionary<string, AppDefinition>(StringComparer.OrdinalIgnoreCase);

        var builtInPath = Path.Combine(AppContext.BaseDirectory, "Definitions", "app-definitions.json");
        MergeFrom(builtInPath, result);

        var userDirectory = Path.Combine(AppPaths.RootFolder, "definitions");
        if (Directory.Exists(userDirectory))
        {
            foreach (var file in Directory.EnumerateFiles(userDirectory, "*.json"))
            {
                MergeFrom(file, result);
            }
        }

        return result.Values.ToList();
    }

    private static void MergeFrom(string filePath, Dictionary<string, AppDefinition> result)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var definitions = JsonSerializer.Deserialize<List<AppDefinition>>(json, JsonOptions);
            if (definitions is null)
            {
                return;
            }

            foreach (var definition in definitions)
            {
                result[definition.Key] = definition;
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // Un fichier de définitions invalide ne doit jamais empêcher le démarrage de l'application.
        }
    }
}
