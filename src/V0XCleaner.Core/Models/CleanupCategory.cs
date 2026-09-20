namespace V0XCleaner.Core.Models;

/// <summary>
/// Catégorie fonctionnelle d'un élément détecté par un scanner.
/// Sert à regrouper les résultats dans l'UI (arborescence à cases à cocher).
/// </summary>
public enum CleanupCategory
{
    SystemTemp,
    RecycleBin,
    Logs,
    ThumbnailCache,
    MemoryDumps,
    ClipboardAndMru,
    BrowserCache,
    BrowserCookies,
    BrowserHistory,
    BrowserDownloadsHistory,
    BrowserFormData,
    ThirdPartyApplication,
    Registry,
    Startup,
    Other
}
