using System.Net.Http;
using System.Text.RegularExpressions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.Services;

/// <summary>
/// winget n'affiche aucune barre de progression quand sa sortie est redirigée : on retrouve donc le
/// pourcentage du téléchargement nous-mêmes, en comparant la taille du fichier en cours d'écriture
/// dans %TEMP%\WinGet\&lt;Id&gt;.&lt;version&gt; à la taille annoncée par le serveur (en-tête Content-Length).
/// </summary>
internal static partial class WingetDownloadWatcher
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    /// <summary>Extrait l'adresse de la ligne « Téléchargement en cours https://... ».</summary>
    public static bool TryGetUrl(string line, out string url)
    {
        var match = UrlRegex().Match(line);
        url = match.Success ? match.Value : string.Empty;
        return match.Success;
    }

    public static async Task WatchAsync(
        string packageId,
        string url,
        DateTime startedUtc,
        IProgress<SoftwareUpdateProgress> progress,
        CancellationToken cancellationToken)
    {
        try
        {
            var total = await GetContentLengthAsync(url, cancellationToken);
            var root = Path.Combine(Path.GetTempPath(), "WinGet");

            while (!cancellationToken.IsCancellationRequested)
            {
                var bytes = LargestRecentFileSize(root, packageId, startedUtc);
                double? percent = total is > 0 && bytes > 0 ? Math.Min(99, bytes * 100.0 / total.Value) : null;
                progress.Report(new SoftwareUpdateProgress(SoftwareUpdatePhase.Downloading, percent));
                await Task.Delay(300, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Fin normale : le téléchargement est terminé ou la mise à jour a été arrêtée.
        }
    }

    internal static async Task<long?> GetContentLengthAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var head = new HttpRequestMessage(HttpMethod.Head, url);
            using var headResponse = await Http.SendAsync(head, cancellationToken);
            if (headResponse.IsSuccessStatusCode && headResponse.Content.Headers.ContentLength is > 0)
            {
                return headResponse.Content.Headers.ContentLength;
            }

            // Certains serveurs refusent HEAD : on lit uniquement les en-têtes d'une requête GET.
            using var get = new HttpRequestMessage(HttpMethod.Get, url);
            using var getResponse = await Http.SendAsync(get, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return getResponse.IsSuccessStatusCode ? getResponse.Content.Headers.ContentLength : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>Taille du plus gros fichier écrit depuis le début de la mise à jour dans les dossiers de téléchargement du paquet.</summary>
    internal static long LargestRecentFileSize(string root, string packageId, DateTime startedUtc)
    {
        long largest = 0;
        try
        {
            if (!Directory.Exists(root))
            {
                return 0;
            }

            foreach (var directory in Directory.EnumerateDirectories(root, packageId + ".*"))
            {
                foreach (var file in new DirectoryInfo(directory).EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    // Un fichier laissé par un essai précédent ne doit pas fausser le pourcentage.
                    if (file.LastWriteTimeUtc >= startedUtc.AddSeconds(-2))
                    {
                        largest = Math.Max(largest, file.Length);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Dossier en cours de création ou de suppression : on réessaiera au prochain relevé.
        }

        return largest;
    }

    [GeneratedRegex(@"https?://\S+")]
    private static partial Regex UrlRegex();
}
