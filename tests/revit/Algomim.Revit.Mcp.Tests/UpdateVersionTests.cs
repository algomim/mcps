using Algomim.Aec.Mcp.Tooling.Updates;
using Xunit;

namespace Algomim.Revit.Mcp.Tests;

public sealed class UpdateVersionTests
{
    [Theory]
    [InlineData("v0.1.9", "0.1.9")]
    [InlineData("0.1.9.0", "0.1.9")]
    [InlineData(" 1.2.3 ", "1.2.3")]
    public void NormalizeVersionReturnsSemVerCore(string input, string expected)
    {
        Assert.Equal(expected, GitHubReleaseUpdateChecker.NormalizeVersion(input));
    }

    [Theory]
    [InlineData("0.1.9", "0.1.8", true)]
    [InlineData("0.2.0", "0.1.9", true)]
    [InlineData("1.0.0", "0.9.9", true)]
    [InlineData("0.1.8", "0.1.8.0", false)]
    [InlineData("0.1.7", "0.1.8", false)]
    public void IsNewerComparesSemVerCore(string latest, string current, bool expected)
    {
        Assert.Equal(expected, GitHubReleaseUpdateChecker.IsNewer(latest, current));
    }

    [Fact]
    public void NoPublishedReleaseFoundReturnsHelpfulMessage()
    {
        var update = ReleaseUpdateInfo.NoPublishedReleaseFound("0.1.9", "https://github.com/algomim/mcps/releases");

        Assert.False(update.IsUpdateAvailable);
        Assert.Equal("0.1.9", update.LatestVersion);
        Assert.Contains("No published GitHub release", update.Message);
    }

    [Fact]
    public void UpdateCheckerSelectsHostSpecificMsiAsset()
    {
        var update = EvaluateReleaseJson(
            """
            {
              "tag_name": "v0.1.23",
              "html_url": "https://github.com/algomim/mcps/releases/tag/v0.1.23",
              "assets": [
                { "name": "revit-mcp-0.1.23.msi", "browser_download_url": "https://example.test/revit.msi" },
                { "name": "autocad-mcp-0.1.23.msi", "browser_download_url": "https://example.test/autocad.msi" },
                { "name": "autocad-mcp-0.1.23.msi.sha256", "browser_download_url": "https://example.test/autocad.sha256" }
              ]
            }
            """,
            "0.1.22",
            "autocad-mcp-",
            "https://github.com/algomim/mcps/releases/latest");

        Assert.True(update.IsUpdateAvailable);
        Assert.Equal("0.1.23", update.LatestVersion);
        Assert.Equal("autocad-mcp-0.1.23.msi", update.InstallerName);
        Assert.Equal("https://example.test/autocad.msi", update.InstallerUrl);
    }

    [Fact]
    public void UpdateCheckerDoesNotAdvertiseUpdateWithoutHostSpecificMsi()
    {
        var update = EvaluateReleaseJson(
            """
            {
              "tag_name": "v0.1.23",
              "html_url": "https://github.com/algomim/mcps/releases/tag/v0.1.23",
              "assets": [
                { "name": "revit-mcp-0.1.23.msi", "browser_download_url": "https://example.test/revit.msi" },
                { "name": "autocad-mcp-0.1.23.msi", "browser_download_url": "https://example.test/autocad.msi" }
              ]
            }
            """,
            "0.1.22",
            "rhino-mcp-",
            "https://github.com/algomim/mcps/releases/latest");

        Assert.False(update.IsUpdateAvailable);
        Assert.Equal("0.1.23", update.LatestVersion);
        Assert.Null(update.InstallerName);
        Assert.Contains("no MSI asset matching 'rhino-mcp-*.msi'", update.Message);
    }

    private static ReleaseUpdateInfo EvaluateReleaseJson(
        string releaseJson,
        string currentVersion,
        string installerAssetPrefix,
        string fallbackReleaseUrl)
    {
        var method = typeof(GitHubReleaseUpdateChecker).GetMethod(
            "EvaluateReleaseJson",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        Assert.NotNull(method);
        return Assert.IsType<ReleaseUpdateInfo>(method.Invoke(
            null,
            [releaseJson, currentVersion, installerAssetPrefix, fallbackReleaseUrl]));
    }

}
