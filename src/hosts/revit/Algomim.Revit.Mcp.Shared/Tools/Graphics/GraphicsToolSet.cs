using Autodesk.Revit.DB;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Composition;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Graphics.ElementOverridesSet;
using RevitView = Autodesk.Revit.DB.View;

namespace Algomim.Revit.Mcp.Tools.Graphics;

internal static class GraphicsToolSet
{
    public static IEnumerable<IMcpTool> Create(RevitToolServices services)
    {
        yield return Read(services, "graphics_get_element_overrides", "Get element-specific graphic overrides in a view.", Schema.From(new { type = "object", properties = new { viewId = new { type = "integer" }, elementIds = new { type = "array", items = new { type = "integer" } } }, required = new[] { "viewId", "elementIds" } }), GetElementOverrides);
        yield return Read(services, "graphics_get_view_filters", "Get filters applied to views.", Schema.From(new { type = "object", properties = new { viewIds = new { type = "array", items = new { type = "integer" } } }, required = new[] { "viewIds" } }), GetViewFilters);
        yield return Read(services, "graphics_get_filter_overrides", "Get graphic overrides for view filters in a view.", Schema.From(new { type = "object", properties = new { viewId = new { type = "integer" }, filterIds = new { type = "array", items = new { type = "integer" } } }, required = new[] { "viewId", "filterIds" } }), GetFilterOverrides);
        yield return Read(services, "graphics_filter_test_elements", "Test whether elements pass a parameter filter.", Schema.From(new { type = "object", properties = new { filterId = new { type = "integer" }, elementIds = new { type = "array", items = new { type = "integer" } } }, required = new[] { "filterId", "elementIds" } }), FilterTestElements);
        yield return ElementOverridesSetTool.Create(services);
    }

    private static IMcpTool Read(RevitToolServices services, string name, string description, System.Text.Json.JsonElement schema, Func<RevitToolContext, ArgumentReader, McpToolResult> execute)
        => Tool(services, name, description, schema, ToolMode.Read, ToolRisk.Low, execute);

    private static IMcpTool Tool(RevitToolServices services, string name, string description, System.Text.Json.JsonElement schema, ToolMode mode, ToolRisk risk, Func<RevitToolContext, ArgumentReader, McpToolResult> execute)
        => new DelegateRevitTool(services.Dispatcher, services.DocumentContextStore, name, description, schema, new ToolMetadata(name, ToolCategory.Graphics, mode, risk, description), (uiApp, args) =>
        {
            var context = RevitToolBase.TryCreateContext(uiApp, services.DocumentContextStore, out var error);
            return context is null ? error : execute(context, args);
        });

    private static McpToolResult GetElementOverrides(RevitToolContext context, ArgumentReader args)
    {
        var view = context.Document.GetElement(RevitIds.Id(args.RequireLong("viewId"))) as RevitView;
        if (view is null) return ToolResults.Error("VIEW_NOT_FOUND", "View not found.");

        var ids = args.RequireLongArray("elementIds", 100);
        var results = ids.Select(id => new
        {
            elementId = id,
            overrides = RevitShapes.OverrideGraphicSettings(view.GetElementOverrides(RevitIds.Id(id))),
        }).ToList();

        return ToolResults.Success(new { count = results.Count, elements = results }, $"{results.Count} element override result(s).");
    }

    private static McpToolResult GetViewFilters(RevitToolContext context, ArgumentReader args)
    {
        var viewIds = args.RequireLongArray("viewIds", 50);
        var views = viewIds.Select(id =>
        {
            var view = context.Document.GetElement(RevitIds.Id(id)) as RevitView;
            var filters = view?.GetFilters()
                .Select(filterId => context.Document.GetElement(filterId))
                .Where(filter => filter is not null)
                .Select(filter => new
                {
                    id = filter!.Id.Value,
                    name = filter.Name,
                    className = filter.GetType().FullName,
                    categories = filter is ParameterFilterElement parameterFilter
                        ? parameterFilter.GetCategories().Select(categoryId => categoryId.Value).ToList()
                        : [],
                })
                .ToList();

            return new { viewId = id, viewName = view?.Name, filterCount = filters?.Count ?? 0, filters };
        }).ToList();

        return ToolResults.Success(new { count = views.Count, views }, $"{views.Count} view filter result(s).");
    }

    private static McpToolResult GetFilterOverrides(RevitToolContext context, ArgumentReader args)
    {
        var view = context.Document.GetElement(RevitIds.Id(args.RequireLong("viewId"))) as RevitView;
        if (view is null) return ToolResults.Error("VIEW_NOT_FOUND", "View not found.");

        var ids = args.RequireLongArray("filterIds", 100);
        var filters = ids.Select(id => new
        {
            filterId = id,
            filterName = context.Document.GetElement(RevitIds.Id(id))?.Name,
            overrides = RevitShapes.OverrideGraphicSettings(view.GetFilterOverrides(RevitIds.Id(id))),
        }).ToList();

        return ToolResults.Success(new { count = filters.Count, filters }, $"{filters.Count} filter override result(s).");
    }

    private static McpToolResult FilterTestElements(RevitToolContext context, ArgumentReader args)
    {
        var filter = context.Document.GetElement(RevitIds.Id(args.RequireLong("filterId"))) as ParameterFilterElement;
        if (filter is null) return ToolResults.Error("FILTER_NOT_FOUND", "Parameter filter not found.");

        var elementFilter = filter.GetElementFilter();
        var ids = args.RequireLongArray("elementIds", 1000);
        var results = ids.Select(id =>
        {
            var element = context.Document.GetElement(RevitIds.Id(id));
            return new { elementId = id, passes = element is not null && elementFilter.PassesFilter(element) };
        }).ToList();

        return ToolResults.Success(new { count = results.Count, elements = results }, $"{results.Count} filter test result(s).");
    }

}
