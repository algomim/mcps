using Algomim.Aec.Mcp.Tooling.Registry;
using Algomim.Rhino.Mcp.Hosting;
using Algomim.Rhino.Mcp.Tools;
using Xunit;

namespace Algomim.Rhino.Mcp.Tests;

public sealed class RhinoArchitectureTests
{
    [Fact]
    public void RhinoServerProfileIsRhinoCommonPluginFirst()
    {
        var profile = RhinoMcpServerProfile.Create();

        Assert.Equal("rhino", profile.Owner);
        Assert.Equal("rhino-mcp", profile.ServerName);
        Assert.Contains("C#/.NET plugin", profile.ServerInstructions);
        Assert.Contains("RhinoCommon", profile.ServerInstructions);
    }

    [Fact]
    public void RhinoInitialToolManifestContainsTypedModelingTools()
    {
        Assert.Equal(76, RhinoToolNames.InitialCatalog.Count);
        Assert.Equal("document_get_info", RhinoToolNames.InitialCatalog[0]);
        Assert.Contains("document_save_as", RhinoToolNames.InitialCatalog);
        Assert.Contains("command_run", RhinoToolNames.InitialCatalog);
        Assert.Contains("geometry_create_box", RhinoToolNames.InitialCatalog);
        Assert.Contains("geometry_create_sphere", RhinoToolNames.InitialCatalog);
        Assert.Contains("geometry_create_cylinder", RhinoToolNames.InitialCatalog);
        Assert.Contains("geometry_boolean_union", RhinoToolNames.InitialCatalog);
        Assert.Contains("geometry_extrude_curve", RhinoToolNames.InitialCatalog);
        Assert.Contains("geometry_create_sweep1", RhinoToolNames.InitialCatalog);
        Assert.Contains("entity_move", RhinoToolNames.InitialCatalog);
        Assert.Contains("entity_analyze", RhinoToolNames.InitialCatalog);
        Assert.Contains("layer_create", RhinoToolNames.InitialCatalog);
        Assert.Contains("selection_set", RhinoToolNames.InitialCatalog);
        Assert.Contains("view_zoom_to_entity", RhinoToolNames.InitialCatalog);
        Assert.Contains("view_capture_snapshot", RhinoToolNames.InitialCatalog);
        Assert.Contains("grasshopper_component_add", RhinoToolNames.InitialCatalog);
        Assert.Contains("grasshopper_component_connect", RhinoToolNames.InitialCatalog);
        Assert.Contains("grasshopper_graph_build", RhinoToolNames.InitialCatalog);
        Assert.Contains("grasshopper_graph_mutate", RhinoToolNames.InitialCatalog);
        Assert.Contains("grasshopper_preview_capture", RhinoToolNames.InitialCatalog);

        var errors = ToolCatalogValidator.ValidateNames(RhinoToolNames.InitialCatalog);
        Assert.Empty(errors);
    }

    [Fact]
    public void RhinoLifecycleSkeletonFollowsHostContract()
    {
        var root = FindRepositoryRoot();
        var plugin = File.ReadAllText(Path.Combine(root, "src", "hosts", "rhino", "Algomim.Rhino.Mcp.Shared", "App", "RhinoMcpPlugin.cs"));
        var commands = File.ReadAllText(Path.Combine(root, "src", "hosts", "rhino", "Algomim.Rhino.Mcp.Shared", "App", "RhinoMcpCommands.cs"));
        var project = File.ReadAllText(Path.Combine(root, "src", "hosts", "rhino", "Algomim.Rhino.Mcp.8", "Algomim.Rhino.Mcp.8.csproj"));
        var catalog = File.ReadAllText(Path.Combine(root, "src", "hosts", "rhino", "Algomim.Rhino.Mcp.Shared", "Tools", "RhinoToolCatalog.cs"));
        var documentTools = File.ReadAllText(Path.Combine(root, "src", "hosts", "rhino", "Algomim.Rhino.Mcp.Shared", "Tools", "Document", "DocumentToolSet.cs"));
        var layerTools = File.ReadAllText(Path.Combine(root, "src", "hosts", "rhino", "Algomim.Rhino.Mcp.Shared", "Tools", "Layer", "LayerToolSet.cs"));
        var geometryTools = File.ReadAllText(Path.Combine(root, "src", "hosts", "rhino", "Algomim.Rhino.Mcp.Shared", "Tools", "Geometry", "GeometryToolSet.cs"));
        var entityTools = File.ReadAllText(Path.Combine(root, "src", "hosts", "rhino", "Algomim.Rhino.Mcp.Shared", "Tools", "Entity", "EntityToolSet.cs"));
        var selectionTools = File.ReadAllText(Path.Combine(root, "src", "hosts", "rhino", "Algomim.Rhino.Mcp.Shared", "Tools", "Selection", "SelectionToolSet.cs"));
        var viewTools = File.ReadAllText(Path.Combine(root, "src", "hosts", "rhino", "Algomim.Rhino.Mcp.Shared", "Tools", "View", "ViewToolSet.cs"));
        var commandTools = File.ReadAllText(Path.Combine(root, "src", "hosts", "rhino", "Algomim.Rhino.Mcp.Shared", "Tools", "Command", "CommandToolSet.cs"));
        var grasshopperTools = File.ReadAllText(Path.Combine(root, "src", "hosts", "rhino", "Algomim.Rhino.Mcp.Shared", "Tools", "Grasshopper", "GrasshopperToolSet.cs"));
        var uiThread = File.ReadAllText(Path.Combine(root, "src", "hosts", "rhino", "Algomim.Rhino.Mcp.Shared", "Tools", "Common", "RhinoUiThread.cs"));

        Assert.Contains("MCP URL: http://127.0.0.1:{port}/mcp", plugin);
        Assert.Contains("Health: http://127.0.0.1:{port}/health", plugin);
        Assert.Contains("_announcements?.Write", plugin);
        Assert.Contains("_announcements?.Remove(RhinoMcpServerProfile.Owner, Environment.ProcessId)", plugin);
        Assert.Contains(".CheckAsync(currentVersion, \"rhino-mcp-\")", plugin);
        Assert.Contains("EnglishName => \"Algomim\"", commands);
        Assert.Contains("app.IsConnected ? -1 : options.AddOption(\"Connect\")", commands);
        Assert.Contains("app.IsConnected ? options.AddOption(\"Disconnect\") : -1", commands);
        Assert.Contains("AddOption(\"Status\")", commands);
        Assert.Contains("AddOption(\"Update\")", commands);
        Assert.DoesNotContain("AlgomimMcpConnect", commands);
        Assert.DoesNotContain("AlgomimMcpDisconnect", commands);
        Assert.DoesNotContain("AlgomimMcpStatus", commands);
        Assert.DoesNotContain("AlgomimMcpCheckUpdate", commands);
        Assert.DoesNotContain("AlgomimMcpToggle", commands);
        Assert.Contains("<TargetExt>.rhp</TargetExt>", project);
        Assert.Contains("<GrasshopperReferencePath>", project);
        Assert.Contains("..DocumentToolSet.Create()", catalog);
        Assert.Contains("..LayerToolSet.Create()", catalog);
        Assert.Contains("..GeometryToolSet.Create()", catalog);
        Assert.Contains("..EntityToolSet.Create()", catalog);
        Assert.Contains("..SelectionToolSet.Create()", catalog);
        Assert.Contains("..ViewToolSet.Create()", catalog);
        Assert.Contains("..CommandToolSet.Create()", catalog);
        Assert.Contains("..GrasshopperToolSet.Create()", catalog);
        Assert.Contains("\"document_get_info\"", documentTools);
        Assert.Contains("\"document_save_as\"", documentTools);
        Assert.Contains("\"layer_create\"", layerTools);
        Assert.Contains("\"geometry_create_box\"", geometryTools);
        Assert.Contains("\"geometry_boolean_union\"", geometryTools);
        Assert.Contains("\"geometry_create_sweep1\"", geometryTools);
        Assert.Contains("\"entity_query\"", entityTools);
        Assert.Contains("\"entity_move\"", entityTools);
        Assert.Contains("\"entity_analyze\"", entityTools);
        Assert.Contains("\"selection_get\"", selectionTools);
        Assert.Contains("\"selection_set\"", selectionTools);
        Assert.Contains("\"view_capture_snapshot\"", viewTools);
        Assert.Contains("\"view_zoom_to_entity\"", viewTools);
        Assert.Contains("\"command_run\"", commandTools);
        Assert.Contains("\"grasshopper_component_add\"", grasshopperTools);
        Assert.Contains("\"grasshopper_graph_build\"", grasshopperTools);
        Assert.Contains("\"grasshopper_graph_mutate\"", grasshopperTools);
        Assert.Contains("RhinoApp.InvokeOnUiThread", uiThread);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Algomim.Aec.Mcp.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be found.");
    }
}
