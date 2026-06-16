using Autodesk.Revit.DB;
using Algomim.Aec.Mcp.Core.Operations.Create;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;
using RevitView = Autodesk.Revit.DB.View;

namespace Algomim.Revit.Mcp.Tools.Create.TagCreate;

internal sealed class RevitTagCreateExecutor
{
    public McpToolResult Execute(RevitToolContext context, TagCreatePlan plan)
    {
        var viewId = RevitIds.Id(plan.ViewId);
        var tags = new List<object>();
        foreach (var id in plan.ElementIds)
        {
            var element = context.Document.GetElement(RevitIds.Id(id));
            if (element is null) continue;

            var box = element.get_BoundingBox(context.Document.GetElement(viewId) as RevitView);
            var point = box is null ? XYZ.Zero : (box.Min + box.Max) / 2 + new XYZ(plan.OffsetX, plan.OffsetY, 0);
            var tag = IndependentTag.Create(context.Document, viewId, new Reference(element), plan.AddLeader, TagMode.TM_ADDBY_CATEGORY, TagOrientation.Horizontal, point);
            tags.Add(RevitElementSummary.FromElement(tag));
        }

        return ToolResults.Success(new { count = tags.Count, tags }, $"{tags.Count} tag(s) created.");
    }
}
