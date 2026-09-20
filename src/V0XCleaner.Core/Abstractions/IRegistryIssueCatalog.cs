using V0XCleaner.Core.Models;

namespace V0XCleaner.Core.Abstractions;

/// <summary>Fournit la liste des problèmes de registre détectables (page Registre), séparée du catalogue du Nettoyeur.</summary>
public interface IRegistryIssueCatalog
{
    IReadOnlyList<CleaningTask> GetTasks();
}
