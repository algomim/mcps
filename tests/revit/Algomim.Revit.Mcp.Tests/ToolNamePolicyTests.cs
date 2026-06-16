using Algomim.Aec.Mcp.Core.Naming;
using Algomim.Aec.Mcp.Tooling.Registry;
using Xunit;

namespace Algomim.Revit.Mcp.Tests;

public class ToolNamePolicyTests
{
    [Fact]
    public void Canonical_domain_first_names_are_valid()
    {
        Assert.True(ToolNamePolicy.Validate("element_move").IsValid);
        Assert.True(ToolNamePolicy.Validate("view_capture_snapshot").IsValid);
        Assert.True(ToolNamePolicy.Validate("level_create").IsValid);
        Assert.True(ToolNamePolicy.Validate("view_create_plans").IsValid);
        Assert.True(ToolNamePolicy.Validate("export_pdf").IsValid);
    }

    [Fact]
    public void Action_first_create_names_are_not_canonical()
    {
        Assert.False(ToolNamePolicy.Validate("create_levels").IsValid);
        Assert.False(ToolNamePolicy.Validate("create_sheets").IsValid);
        Assert.False(ToolNamePolicy.Validate("create_schedule").IsValid);
    }

    [Fact]
    public void Uppercase_or_dash_names_are_invalid()
    {
        Assert.False(ToolNamePolicy.Validate("ElementMove").IsValid);
        Assert.False(ToolNamePolicy.Validate("element-move").IsValid);
    }

    [Fact]
    public void Unsupported_domains_are_invalid()
    {
        var result = ToolNamePolicy.Validate("banana_move");

        Assert.False(result.IsValid);
        Assert.Equal("TOOL_NAME_DOMAIN", result.Code);
    }

    [Fact]
    public void Catalog_validator_detects_duplicates()
    {
        var errors = ToolCatalogValidator.ValidateNames(["element_move", "element_move"]);

        Assert.Contains(errors, error => error.Contains("DUPLICATE_TOOL_NAME"));
    }
}
