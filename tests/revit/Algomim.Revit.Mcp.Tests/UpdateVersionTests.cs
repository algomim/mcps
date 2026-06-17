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

}
