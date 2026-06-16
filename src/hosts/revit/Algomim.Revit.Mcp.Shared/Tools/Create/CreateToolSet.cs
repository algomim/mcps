using Autodesk.Revit.DB;
using Algomim.Revit.Mcp.Harness;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Composition;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Create.DraftingOrLegendViewCreate;
using Algomim.Revit.Mcp.Tools.Create.GridCreate;
using Algomim.Revit.Mcp.Tools.Create.LevelCreate;
using Algomim.Revit.Mcp.Tools.Create.RoomElevationCreate;
using Algomim.Revit.Mcp.Tools.Create.ScheduleCreate;
using Algomim.Revit.Mcp.Tools.Create.SheetCreate;
using Algomim.Revit.Mcp.Tools.Create.TagCreate;
using Algomim.Revit.Mcp.Tools.Create.TextNotesCreate;
using Algomim.Revit.Mcp.Tools.Create.View3DCreate;
using Algomim.Revit.Mcp.Tools.Create.ViewPlanCreate;
using RevitElement = Autodesk.Revit.DB.Element;
using Algomim.Revit.Mcp.Tools.Create.ViewSectionCreate;

namespace Algomim.Revit.Mcp.Tools.Create;

internal static class CreateToolSet
{
    public static IEnumerable<IMcpTool> Create(RevitToolServices services)
    {
        yield return GridCreateTool.Create(services);
        yield return LevelCreateTool.Create(services);
        yield return ViewPlanCreateTool.Create(services);
        yield return View3DCreateTool.Create(services);
        yield return ViewSectionCreateTool.Create(services);
        yield return SheetCreateTool.Create(services);
        yield return Tool(services, "sheet_place_views", "Place views or schedules on a sheet at XYZ centers.", Schema.From(new { type = "object", properties = new { sheetId = new { type = "integer" }, viewIds = IntegerArray(), x = NumberArray(), y = NumberArray(), z = NumberArray() }, required = new[] { "sheetId", "viewIds", "x", "y", "z" } }), SheetPlaceViews);
        yield return ScheduleCreateTool.Create(services);
        yield return DraftingOrLegendViewCreateTool.Create(services);
        yield return TextNotesCreateTool.Create(services);
        yield return RoomElevationCreateTool.Create(services);
        yield return TagCreateTool.Create(services);
    }

    private static IMcpTool Tool(RevitToolServices services, string name, string description, System.Text.Json.JsonElement schema, Func<RevitToolContext, ArgumentReader, McpToolResult> execute, ToolCategory category = ToolCategory.Create)
        => new DelegateRevitTool(services.Dispatcher, services.DocumentContextStore, name, description, schema, new ToolMetadata(name, category, ToolMode.Write, ToolRisk.High, description), (uiApp, args) =>
        {
            var context = RevitToolBase.TryCreateContext(uiApp, services.DocumentContextStore, out var error);
            return context is null ? error : Write(context, name, () => execute(context, args));
        });

    private static McpToolResult SheetPlaceViews(RevitToolContext context, ArgumentReader args)
    {
        var sheetId = RevitIds.Id(args.RequireLong("sheetId"));
        var viewIds = args.RequireLongArray("viewIds", 500);
        var x = args.RequireDoubleArray("x", 500);
        var y = args.RequireDoubleArray("y", 500);
        var z = args.RequireDoubleArray("z", 500);
        var placed = new List<object>();
        for (var i = 0; i < viewIds.Count; i++)
        {
            var viewId = RevitIds.Id(viewIds[i]);
            var center = RevitListAccess.VectorAt(x, y, z, i);
            RevitElement placedElement = context.Document.GetElement(viewId) is ViewSchedule
                ? ScheduleSheetInstance.Create(context.Document, sheetId, viewId, center)
                : Viewport.Create(context.Document, sheetId, viewId, center);
            placed.Add(RevitElementSummary.FromElement(placedElement));
        }

        return ToolResults.Success(new { count = placed.Count, placed }, $"{placed.Count} view/schedule item(s) placed.");
    }

    private static McpToolResult Write(RevitToolContext context, string name, Func<McpToolResult> action)
    {
        if (context.IsLinkedDocument)
            return ToolResults.Error("LINKED_DOCUMENT_WRITE_BLOCKED", "Cannot write to linked document context.");

        return TransactionRunner.Run(context.Document, $"revit-mcp {name}", action);
    }

    private static object NumberArray() => new { type = "array", items = new { type = "number" } };
    private static object IntegerArray() => new { type = "array", items = new { type = "integer" } };

}
