namespace Algomim.Aec.Mcp.Tooling.Updates;

public sealed record ReleaseUpdateInfo(
    string CurrentVersion,
    string LatestVersion,
    bool IsUpdateAvailable,
    string ReleaseUrl,
    string? InstallerUrl,
    string? InstallerName,
    string? Message = null)
{
    public static ReleaseUpdateInfo UpToDate(string currentVersion, string latestVersion, string releaseUrl)
        => new(currentVersion, latestVersion, false, releaseUrl, null, null);

    public static ReleaseUpdateInfo NoPublishedReleaseFound(string currentVersion, string releaseUrl)
        => new(
            currentVersion,
            currentVersion,
            false,
            releaseUrl,
            null,
            null,
            "No published GitHub release was found yet. This can happen before the first release is published or when the repository is private.");

    public static ReleaseUpdateInfo UpdateAvailable(
        string currentVersion,
        string latestVersion,
        string releaseUrl,
        string? installerUrl,
        string? installerName)
        => new(currentVersion, latestVersion, true, releaseUrl, installerUrl, installerName);
}
