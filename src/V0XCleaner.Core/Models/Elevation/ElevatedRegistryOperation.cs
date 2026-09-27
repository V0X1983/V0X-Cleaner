namespace V0XCleaner.Core.Models.Elevation;

public enum ElevatedRegistryOperationKind
{
    DeleteKey,
    DeleteValue,
}

/// <summary>
/// Une opération registre unique nécessitant les droits administrateur, destinée à être exécutée
/// par V0XCleaner.ElevatedHelper. <see cref="OperationId"/> permet de faire correspondre chaque
/// résultat à son <c>CleanupItem.Id</c> d'origine côté appelant.
/// </summary>
/// <param name="Path">Chemin du parent (DeleteKey) ou de la clé contenant la valeur (DeleteValue).</param>
/// <param name="Name">Nom de la sous-clé à supprimer (DeleteKey) ou de la valeur (DeleteValue).</param>
public sealed record ElevatedRegistryOperation(
    string OperationId,
    ElevatedRegistryOperationKind Kind,
    string HivePrefix,
    string Path,
    string Name);
