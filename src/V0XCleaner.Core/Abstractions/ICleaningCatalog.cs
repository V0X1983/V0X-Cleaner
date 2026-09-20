using V0XCleaner.Core.Models;

namespace V0XCleaner.Core.Abstractions;

/// <summary>Fournit la liste complète des tâches de nettoyage disponibles sur cette machine.</summary>
public interface ICleaningCatalog
{
    IReadOnlyList<CleaningTask> GetTasks();
}
