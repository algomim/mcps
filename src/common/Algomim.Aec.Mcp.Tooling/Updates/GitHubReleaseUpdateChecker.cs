using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Algomim.Aec.Mcp.Tooling.Updates;

public sealed class GitHubReleaseUpdateChecker
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    private readonly Uri _latestReleaseUrl;
    private readonly Uri _releasesPageUrl;
    private readonly string _userAgent;

    public GitHubReleaseUpdateChecker(string owner, string repository, string userAgent = "algomim-aec-mcp")
        : this(
            new Uri($"https://api.github.com/repos/{owner}/{repository}/releases/latest"),
            new Uri($"https://github.com/{owner}/{repository}/releases"),
            userAgent)
    {
    }

    internal GitHubReleaseUpdateChecker(Uri latestReleaseUrl, string userAgent)
        : this(latestReleaseUrl, latestReleaseUrl, userAgent)
    {
    }

    private GitHubReleaseUpdateChecker(Uri latestReleaseUrl, Uri releasesPageUrl, string userAgent)
    {
        _latestReleaseUrl = latestReleaseUrl;
        _releasesPageUrl = releasesPageUrl;
        _userAgent = userAgent;
    }

    public async Task<ReleaseUpdateInfo> CheckAsync(
        string currentVersion,
        string installerAssetPrefix,
        CancellationToken cancellationToken = default)
    {
        var current = NormalizeVersion(currentVersion);

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(_userAgent);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        Stream stream;
        try
        {
            stream = await client.GetStreamAsync(_latestReleaseUrl, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return ReleaseUpdateInfo.NoPublishedReleaseFound(current, _releasesPageUrl.ToString());
        }

        await using (stream)
        {
            var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, Options, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("GitHub release response was empty.");

            return EvaluateRelease(release, current, installerAssetPrefix, _latestReleaseUrl.ToString());
        }
    }

    internal static ReleaseUpdateInfo EvaluateReleaseJson(
        string releaseJson,
        string currentVersion,
        string installerAssetPrefix,
        string fallbackReleaseUrl)
    {
        var release = JsonSerializer.Deserialize<GitHubRelease>(releaseJson, Options)
            ?? throw new InvalidOperationException("GitHub release response was empty.");

        return EvaluateRelease(release, NormalizeVersion(currentVersion), installerAssetPrefix, fallbackReleaseUrl);
    }

    private static ReleaseUpdateInfo EvaluateRelease(
        GitHubRelease release,
        string current,
        string installerAssetPrefix,
        string fallbackReleaseUrl)
    {
        var latest = NormalizeVersion(release.TagName);
        var releaseUrl = string.IsNullOrWhiteSpace(release.HtmlUrl)
            ? fallbackReleaseUrl
            : release.HtmlUrl;

        if (!IsNewer(latest, current))
            return ReleaseUpdateInfo.UpToDate(current, latest, releaseUrl);

        var installer = release.Assets
            .Where(asset => asset.Name.StartsWith(installerAssetPrefix, StringComparison.OrdinalIgnoreCase))
            .Where(asset => asset.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
            .OrderBy(asset => asset.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (installer is null)
            return ReleaseUpdateInfo.NoHostInstallerFound(current, latest, releaseUrl, installerAssetPrefix);

        return ReleaseUpdateInfo.UpdateAvailable(
            current,
            latest,
            releaseUrl,
            installer.BrowserDownloadUrl,
            installer.Name);
    }

    public static string NormalizeVersion(string value)
    {
        var normalized = value.Trim();
        if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[1..];

        var parts = normalized
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(3)
            .Select(part => int.TryParse(part, out var number) ? number : (int?)null)
            .ToArray();

        if (parts.Length < 3 || parts.Any(part => part is null))
            throw new ArgumentException($"Version must use X.Y.Z form, got '{value}'.", nameof(value));

        return string.Join(".", parts.Select(part => part!.Value));
    }

    public static bool IsNewer(string latestVersion, string currentVersion)
    {
        var latest = NormalizeVersion(latestVersion).Split('.').Select(int.Parse).ToArray();
        var current = NormalizeVersion(currentVersion).Split('.').Select(int.Parse).ToArray();

        for (var i = 0; i < latest.Length; i++)
        {
            if (latest[i] > current[i]) return true;
            if (latest[i] < current[i]) return false;
        }

        return false;
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; set; } = new();
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}
