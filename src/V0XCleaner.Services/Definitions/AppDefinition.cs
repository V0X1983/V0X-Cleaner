namespace V0XCleaner.Services.Definitions;

/// <summary>
/// Une entrée du système de "définitions" d'applications tierces (inspiré de winapp2.ini) :
/// un nom d'application et les motifs de chemins (voir PathPatternExpander) désignant son cache.
/// Chargée depuis Definitions/app-definitions.json, extensible par l'utilisateur via
/// %AppData%\V0XCleaner\definitions\*.json.
/// </summary>
public sealed class AppDefinition
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required List<string> Patterns { get; init; }
    /// <summary>"cloud" pour ranger la définition dans la section Stockage cloud du Nettoyeur ; vide = Applications tierces.</summary>
    public string? Section { get; init; }
    public bool SelectedByDefault { get; init; } = true;
}
