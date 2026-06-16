using Autodesk.Revit.DB;
using Algomim.Aec.Mcp.Core.Geometry;
using Algomim.Aec.Mcp.Core.Operations.Modify;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Modify.ElementCopy;

internal sealed class RevitElementCopyExecutor
{
    public McpToolResult Execute(RevitToolContext context, ElementCopyPlan plan)
    {
        var copied = new List<object>();
        foreach (var item in plan.Items)
        {
            var newIds = ElementTransformUtils.CopyElement(context.Document, RevitIds.Id(item.ElementId), ToXyz(item.Offset));
            copied.Add(new { sourceElementId = item.ElementId, copiedElementIds = newIds.Select(id => id.Value).ToList() });
        }

        return ToolResults.Success(new { count = copied.Count, copies = copied }, $"{copied.Count} element copy operation(s).");
    }

    private static XYZ ToXyz(AecPoint3 point)
        => new(point.X, point.Y, point.Z);
}
