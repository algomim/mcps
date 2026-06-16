using Algomim.Revit.Mcp.Discovery;
using Algomim.Revit.Mcp.Harness;
using Algomim.Aec.Mcp.Scripting;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Analysis;
using Algomim.Revit.Mcp.Tools.Api;
using Algomim.Revit.Mcp.Tools.Category;
using Algomim.Revit.Mcp.Tools.Composition;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Document;
using Algomim.Revit.Mcp.Tools.Element;
using Algomim.Revit.Mcp.Tools.Selection;
using Algomim.Revit.Mcp.Tools.View;

namespace Algomim.Revit.Mcp.Tools;

/// <summary>Owns the public MCP tool catalog and its stable ordering.</summary>
public static class RevitToolCatalog
{
    public static IReadOnlyList<IMcpTool> Create(IScriptCompiler compiler, IUiThreadDispatcher dispatcher)
        => Create(new RevitToolServices(dispatcher, compiler, new RevitLogger(), new RevitDocumentContextStore()));

    internal static IReadOnlyList<IMcpTool> Create(RevitToolServices services)
    {
        var scriptExecute = new ExecuteScriptTool(services.ScriptCompiler, services.Dispatcher);
        var discoverApi = new DiscoverApiTool();

        var tools = new List<IMcpTool>
        {
            new ApiDiscoverTool(),
            new McpToolAlias(
                "script_execute",
                "Run C# against the active Revit document. Standard name for the legacy execute-script primitive.",
                scriptExecute),
            new DocumentGetInfoTool(services.Dispatcher, services.DocumentContextStore),
            new ViewGetActiveTool(services.Dispatcher, services.DocumentContextStore),
            new SelectionGetTool(services.Dispatcher, services.DocumentContextStore),
            new CategorySearchTool(services.Dispatcher, services.DocumentContextStore),
            new CategoryListTool(services.Dispatcher, services.DocumentContextStore),
            new ElementListByCategoryTool(services.Dispatcher, services.DocumentContextStore),
            new ElementGetInfoTool(services.Dispatcher, services.DocumentContextStore)
        };

        foreach (var module in RevitToolModuleRegistry.CreateDefaultModules())
        {
            tools.AddRange(module.CreateTools(services));
        }

        AddCanonicalCreateAliases(tools);

        // Backward-compatible legacy names.
        tools.Add(scriptExecute);
        tools.Add(discoverApi);

        GuardUniqueNames(tools);
        return tools;
    }

    private static void GuardUniqueNames(IEnumerable<IMcpTool> tools)
    {
        var duplicate = tools
            .GroupBy(tool => tool.Name, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
            throw new InvalidOperationException($"Duplicate MCP tool name: {duplicate.Key}");
    }

    private static void AddCanonicalCreateAliases(List<IMcpTool> tools)
    {
        AddAlias(tools, "grid_create", "Canonical domain-first alias for create_grids.", "create_grids");
        AddAlias(tools, "level_create", "Canonical domain-first alias for create_levels.", "create_levels");
        AddAlias(tools, "view_create_plans", "Canonical domain-first alias for create_view_plans.", "create_view_plans");
        AddAlias(tools, "view_create_3ds", "Canonical domain-first alias for create_view_3ds.", "create_view_3ds");
        AddAlias(tools, "view_create_sections", "Canonical domain-first alias for create_view_sections.", "create_view_sections");
        AddAlias(tools, "sheet_create", "Canonical domain-first alias for create_sheets.", "create_sheets");
        AddAlias(tools, "schedule_create", "Canonical domain-first alias for create_schedule.", "create_schedule");
        AddAlias(tools, "view_create_drafting_or_legend", "Canonical domain-first alias for create_drafting_or_legend_views.", "create_drafting_or_legend_views");
        AddAlias(tools, "view_create_text_notes", "Canonical domain-first alias for create_text_notes.", "create_text_notes");
        AddAlias(tools, "view_create_room_elevations", "Canonical domain-first alias for create_room_elevation_views.", "create_room_elevation_views");
        AddAlias(tools, "tag_create", "Canonical domain-first alias for create_tags.", "create_tags");
    }

    private static void AddAlias(List<IMcpTool> tools, string name, string description, string targetName)
    {
        var target = tools.FirstOrDefault(tool => tool.Name == targetName)
            ?? throw new InvalidOperationException($"Cannot create alias '{name}'. Target tool '{targetName}' was not registered.");

        tools.Add(new McpToolAlias(name, description, target));
    }
}
