using System.Text.Json;
using Autodesk.Revit.UI;
using Algomim.Revit.Mcp.Harness;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.View;

/// <summary>Returns details for the current active Revit view.</summary>
public sealed class ViewGetActiveTool : RevitToolBase
{
    public ViewGetActiveTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore)
        : base(dispatcher, documentContextStore) { }

    public override string Name => "view_get_active";

    public override string Description => "Get the title, id, type, scale, and orientation facts for the active Revit view.";

    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new { },
    });

    public override ToolMetadata Metadata { get; } = new(
        "view_get_active",
        ToolCategory.View,
        ToolMode.Read,
        ToolRisk.Low,
        "Get active Revit view details.");

    protected override McpToolResult ExecuteOnUiThread(UIApplication uiApp, ArgumentReader arguments)
    {
        var context = TryCreateContext(uiApp, out var error);
        if (context is null) return error;

        var view = context.ActiveView;
        if (view is null)
            return Error("NO_ACTIVE_VIEW", "No active Revit view.");

        var direction = view.ViewDirection;
        var origin = view.Origin;
        var up = view.UpDirection;
        var right = view.RightDirection;

        var data = new
        {
            id = view.Id.Value,
            name = view.Name,
            viewType = view.ViewType.ToString(),
            isTemplate = view.IsTemplate,
            scale = view.Scale,
            origin = new { x = origin.X, y = origin.Y, z = origin.Z },
            viewDirection = new { x = direction.X, y = direction.Y, z = direction.Z },
            upDirection = new { x = up.X, y = up.Y, z = up.Z },
            rightDirection = new { x = right.X, y = right.Y, z = right.Z },
        };

        return Success(data, $"Active view is '{view.Name}'.");
    }
}
