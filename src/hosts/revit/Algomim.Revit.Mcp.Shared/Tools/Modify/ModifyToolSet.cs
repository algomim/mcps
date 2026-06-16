using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Algomim.Revit.Mcp.Harness;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Composition;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Modify.ElementCopy;
using Algomim.Revit.Mcp.Tools.Modify.ElementDelete;
using Algomim.Revit.Mcp.Tools.Modify.ElementMove;
using Algomim.Revit.Mcp.Tools.Modify.ElementRotate;
using Algomim.Revit.Mcp.Tools.Modify.SheetSetRevisions;
using Algomim.Revit.Mcp.Tools.Modify.ViewCopyFilters;
using RevitView = Autodesk.Revit.DB.View;

namespace Algomim.Revit.Mcp.Tools.Modify;

internal static class ModifyToolSet
{
    public static IEnumerable<IMcpTool> Create(RevitToolServices services)
    {
        yield return Tool(services, "selection_set", "Set current Revit UI selection to element ids.", ToolCategory.Selection, ToolMode.Write, ToolRisk.Medium, Schema.From(new { type = "object", properties = new { elementIds = new { type = "array", items = new { type = "integer" } } }, required = new[] { "elementIds" } }), SelectionSet);
        yield return Tool(services, "view_isolate_elements", "Temporarily isolate element ids in a view.", ToolCategory.View, ToolMode.Write, ToolRisk.High, Schema.From(new { type = "object", properties = new { viewId = new { type = "integer" }, elementIds = new { type = "array", items = new { type = "integer" } } }, required = new[] { "viewId", "elementIds" } }), ViewIsolateElements);
        yield return ElementMoveTool.Create(services);
        yield return ElementRotateTool.Create(services);
        yield return ElementDeleteTool.Create(services);
        yield return ElementCopyTool.Create(services);
        yield return ViewCopyFiltersTool.Create(services);
        yield return SheetSetRevisionsTool.Create(services);
    }

    private static IMcpTool Tool(RevitToolServices services, string name, string description, ToolCategory category, ToolMode mode, ToolRisk risk, System.Text.Json.JsonElement schema, Func<RevitToolContext, ArgumentReader, McpToolResult> execute)
        => new DelegateRevitTool(services.Dispatcher, services.DocumentContextStore, name, description, schema, new ToolMetadata(name, category, mode, risk, description), (uiApp, args) =>
        {
            var context = RevitToolBase.TryCreateContext(uiApp, services.DocumentContextStore, out var error);
            return context is null ? error : execute(context, args);
        });

    private static McpToolResult SelectionSet(RevitToolContext context, ArgumentReader args)
    {
        var ids = RevitIds.ToElementIds(args.RequireLongArray("elementIds", 5000));
        context.UiDocument.Selection.SetElementIds(ids);
        return ToolResults.Success(new { count = ids.Count, elementIds = ids.Select(id => id.Value).ToList() }, $"{ids.Count} element(s) selected.");
    }

    private static McpToolResult ViewIsolateElements(RevitToolContext context, ArgumentReader args)
    {
        var view = context.Document.GetElement(RevitIds.Id(args.RequireLong("viewId"))) as RevitView;
        if (view is null) return ToolResults.Error("VIEW_NOT_FOUND", "View not found.");

        var ids = RevitIds.ToElementIds(args.RequireLongArray("elementIds", 5000));
        return Write(context, "view_isolate_elements", () =>
        {
            view.IsolateElementsTemporary(ids);
            return ToolResults.Success(new { viewId = view.Id.Value, count = ids.Count }, $"{ids.Count} element(s) isolated.");
        });
    }

    private static McpToolResult Write(RevitToolContext context, string name, Func<McpToolResult> action)
    {
        if (context.IsLinkedDocument)
            return ToolResults.Error("LINKED_DOCUMENT_WRITE_BLOCKED", "Cannot write to linked document context.");

        return TransactionRunner.Run(context.Document, $"revit-mcp {name}", action);
    }
}
