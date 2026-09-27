namespace V0XCleaner.Core.Models.Elevation;

/// <summary>
/// Contenu du fichier JSON d'entrée lu par V0XCleaner.ElevatedHelper. Une liste vide est valide :
/// elle sert à vérifier que l'élévation (invite UAC) fonctionne sans toucher au registre
/// (utilisé par le bouton « Vérifier l'élévation » de l'app WinUI 3).
/// </summary>
public sealed record ElevatedBatchRequest(IReadOnlyList<ElevatedRegistryOperation> Operations);

/// <summary>Contenu du fichier JSON de sortie écrit par V0XCleaner.ElevatedHelper avant de quitter.</summary>
public sealed record ElevatedBatchResponse(bool Success, string? ErrorMessage, IReadOnlyList<ElevatedOperationResult> Results);
