using Autodesk.Revit.DB;
using Algomim.Aec.Mcp.Core.Operations.Graphics;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;
using RevitView = Autodesk.Revit.DB.View;

namespace Algomim.Revit.Mcp.Tools.Graphics.ElementOverridesSet;

internal sealed class RevitElementOverridesSetExecutor
{
    public McpToolResult Execute(RevitToolContext context, ElementOverridesSetPlan plan)
    {
        var view = context.Document.GetElement(RevitIds.Id(plan.ViewId)) as RevitView;
        if (view is null)
            return ToolResults.Error("VIEW_NOT_FOUND", "View not found.");

        var settings = plan.Clear
            ? new OverrideGraphicSettings()
            : new OverrideGraphicSettings().SetProjectionLineColor(new Color((byte)plan.Red, (byte)plan.Green, (byte)plan.Blue));

        foreach (var id in plan.ElementIds)
            view.SetElementOverrides(RevitIds.Id(id), settings);

        return ToolResults.Success(new { count = plan.ElementIds.Count, elementIds = plan.ElementIds }, $"{plan.ElementIds.Count} element graphic override(s) updated.");
    }
}
