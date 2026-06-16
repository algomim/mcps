using Algomim.Aec.Mcp.Core.Naming;
using Algomim.Aec.Mcp.Tooling.Registry;
using Algomim.AutoCad.Mcp.Hosting;
using Algomim.AutoCad.Mcp.Tools;
using Xunit;

namespace Algomim.AutoCad.Mcp.Tests;

public sealed class AutoCadArchitectureTests
{
    [Theory]
    [InlineData("geometry_create_line")]
    [InlineData("layer_list")]
    [InlineData("entity_move")]
    [InlineData("block_insert")]
    [InlineData("dimension_add_linear")]
    [InlineData("annotation_create_text")]
    [InlineData("drawing_get_extents")]
    [InlineData("measurement_get_area")]
    public void AutoCadToolDomainsFollowSharedPolicy(string name)
    {
        var validation = ToolNamePolicy.Validate(name);

        Assert.True(validation.IsValid, validation.Message);
    }

    [Fact]
    public void AutoCadServerProfileIsCSharpPluginFirst()
    {
        var profile = AutoCadMcpServerProfile.Create();

        Assert.Equal("autocad", profile.Owner);
        Assert.Equal("autocad-mcp", profile.ServerName);
        Assert.Contains("C#/.NET plugin", profile.ServerInstructions);
        Assert.Contains("not Python, LISP, SCR files, or a Node bridge", profile.ServerInstructions);
    }

    [Fact]
    public void AutoCadInitialToolManifestIsNamedAndCounted()
    {
        Assert.Equal(50, AutoCadToolNames.InitialCatalog.Count);
        Assert.Contains("layer_list", AutoCadToolNames.InitialCatalog);
        Assert.Contains("geometry_create_line", AutoCadToolNames.InitialCatalog);
        Assert.Contains("entity_query", AutoCadToolNames.InitialCatalog);
        Assert.Contains("block_insert", AutoCadToolNames.InitialCatalog);
        Assert.Contains("dimension_add_linear", AutoCadToolNames.InitialCatalog);
        Assert.Contains("annotation_create_text", AutoCadToolNames.InitialCatalog);
        Assert.Contains("document_save_as", AutoCadToolNames.InitialCatalog);
        Assert.Contains("export_dxf", AutoCadToolNames.InitialCatalog);

        var errors = ToolCatalogValidator.ValidateNames(AutoCadToolNames.InitialCatalog);
        Assert.Empty(errors);
    }
}
