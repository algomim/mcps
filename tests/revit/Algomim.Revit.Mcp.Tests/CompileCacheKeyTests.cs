using Algomim.Aec.Mcp.Scripting;
using Xunit;

namespace Algomim.Revit.Mcp.Tests;

public class CompileCacheKeyTests
{
    [Fact]
    public void Same_input_yields_same_key()
        => Assert.Equal(CompileCacheKey.Sha256("return doc.Title;"), CompileCacheKey.Sha256("return doc.Title;"));

    [Fact]
    public void Different_input_yields_different_key()
        => Assert.NotEqual(CompileCacheKey.Sha256("a"), CompileCacheKey.Sha256("b"));

    [Fact]
    public void Key_is_hex_of_expected_length()
        => Assert.Equal(64, CompileCacheKey.Sha256("x").Length); // SHA-256 = 32 bytes = 64 hex chars
}
