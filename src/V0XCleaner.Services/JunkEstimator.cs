using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.Services;

/// <summary>
/// Estimation légère de l'espace récupérable, utilisée par la surveillance en arrière-plan :
/// se limite volontairement aux tâches "Système" sélectionnées par défaut (pas les navigateurs
/// ni les applications tierces) pour rester rapide et peu intrusive lors d'un contrôle périodique.
/// </summary>
public sealed class JunkEstimator(ICleaningCatalog catalog) : IJunkEstimator
{
    public async Task<long> EstimateReclaimableBytesAsync(CancellationToken cancellationToken = default)
    {
        long total = 0;

        var tasks = catalog.GetTasks()
            .Where(t => t.SelectedByDefault && t.Section == CleaningSection.System);

        foreach (var task in tasks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await task.Scanner.ScanAsync(cancellationToken);
                total += result.TotalSizeBytes;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Une catégorie inaccessible ne doit pas interrompre l'estimation globale.
            }
        }

        return total;
    }
}
