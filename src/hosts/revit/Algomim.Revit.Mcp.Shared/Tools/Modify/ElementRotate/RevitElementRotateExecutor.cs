using Autodesk.Revit.DB;
using Algomim.Aec.Mcp.Core.Geometry;
using Algomim.Aec.Mcp.Core.Operations.Modify;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Modify.ElementRotate;

internal sealed class RevitElementRotateExecutor
{
    public McpToolResult Execute(RevitToolContext context, ElementRotatePlan plan)
    {
        foreach (var item in plan.Items)
            ElementTransformUtils.RotateElement(context.Document, RevitIds.Id(item.ElementId), Axis(item.AxisStart, item.AxisEnd), item.Angle);

        var ids = plan.Items.Select(item => item.ElementId).ToList();
        return ToolResults.Success(new { count = ids.Count, elementIds = ids }, $"{ids.Count} element(s) rotated.");
    }

    private static Line Axis(AecPoint3 start, AecPoint3 end)
        => Line.CreateBound(ToXyz(start), ToXyz(end));

    private static XYZ ToXyz(AecPoint3 point)
        => new(point.X, point.Y, point.Z);
}
