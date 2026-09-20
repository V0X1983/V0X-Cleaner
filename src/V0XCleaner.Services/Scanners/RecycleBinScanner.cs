using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.Native;

namespace V0XCleaner.Services.Scanners;

/// <summary>Interroge la taille actuelle de la Corbeille via l'API Shell Windows.</summary>
public sealed class RecycleBinScanner : IScanner
{
    public string Key => "recycle-bin";

    public CleanupCategory Category => CleanupCategory.RecycleBin;

    public Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var size = Shell32Interop.GetRecycleBinSizeBytes();

        var items = size > 0
            ? new List<CleanupItem>
            {
                new()
                {
                    Id = "recycle-bin",
                    DisplayPath = "Corbeille",
                    Category = Category,
                    SizeBytes = size,
                    Description = "Contenu actuel de la Corbeille (tous lecteurs)."
                }
            }
            : [];

        return Task.FromResult(new ScanResult { Items = items });
    }
}
