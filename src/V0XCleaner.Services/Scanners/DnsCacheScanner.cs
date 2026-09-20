using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.Services.Scanners;

/// <summary>Élément synthétique représentant le cache de résolution DNS du système.</summary>
public sealed class DnsCacheScanner : IScanner
{
    public string Key => "dns-cache";

    public CleanupCategory Category => CleanupCategory.DnsCache;

    public Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<CleanupItem>
        {
            new()
            {
                Id = "dns-cache",
                DisplayPath = "Cache de résolution DNS",
                Category = Category,
                SizeBytes = 0,
                Description = "Résolutions DNS mises en cache par Windows (ipconfig /flushdns)."
            }
        };

        return Task.FromResult(new ScanResult { Items = items });
    }
}
