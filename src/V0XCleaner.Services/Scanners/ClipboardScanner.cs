using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.Native;

namespace V0XCleaner.Services.Scanners;

/// <summary>Élément synthétique unique représentant le contenu actuel du presse-papiers, s'il n'est pas vide.</summary>
public sealed class ClipboardScanner : IScanner
{
    public string Key => "clipboard";

    public CleanupCategory Category => CleanupCategory.ClipboardAndMru;

    public Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<CleanupItem>();

        if (User32Interop.HasClipboardContent())
        {
            items.Add(new CleanupItem
            {
                Id = "clipboard",
                DisplayPath = "Presse-papiers",
                Category = Category,
                SizeBytes = 0,
                Description = "Contenu actuellement copié dans le presse-papiers."
            });
        }

        return Task.FromResult(new ScanResult { Items = items });
    }
}
