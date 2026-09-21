using System.Net.Http;
using System.Text.Json;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.Services;

/// <summary>
/// Vérifie la dernière publication GitHub Releases du dépôt configuré dans les Options (par défaut
/// le dépôt officiel, voir <see cref="AppSettings.DefaultUpdateOwner"/>). La vérification n'a lieu
/// que lorsque l'utilisateur la demande.
/// </summary>
public sealed class GitHubUpdateChecker : IUpdateChecker
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    public async Task<LatestReleaseInfo> GetLatestReleaseAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(owner))
        {
            owner = AppSettings.DefaultUpdateOwner;
        }

        if (string.IsNullOrWhiteSpace(repo))
        {
            repo = AppSettings.DefaultUpdateRepo;
        }

        owner = owner.Trim();
        repo = repo.Trim();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}/releases/latest");
            request.Headers.UserAgent.ParseAdd("V0XCleaner-UpdateChecker");

            using var response = await Http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new LatestReleaseInfo(false, null, null, "Aucune version publiée pour le moment sur le dépôt de mise à jour.");
                }

                return new LatestReleaseInfo(false, null, null, $"Impossible de vérifier les mises à jour (code {(int)response.StatusCode}).");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var tagName = doc.RootElement.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() : null;
            var htmlUrl = doc.RootElement.TryGetProperty("html_url", out var urlProp) ? urlProp.GetString() : null;

            if (string.IsNullOrWhiteSpace(tagName))
            {
                return new LatestReleaseInfo(false, null, null, "Réponse inattendue du serveur de mises à jour.");
            }

            var version = tagName.TrimStart('v', 'V');
            return new LatestReleaseInfo(true, version, htmlUrl, $"Dernière version publiée : {version}.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new LatestReleaseInfo(false, null, null, $"Échec de la vérification : {ex.Message}");
        }
    }
}
