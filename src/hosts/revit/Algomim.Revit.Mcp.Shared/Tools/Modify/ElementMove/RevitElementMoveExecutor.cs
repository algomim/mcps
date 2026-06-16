using Autodesk.Revit.DB;
using Algomim.Aec.Mcp.Core.Geometry;
using Algomim.Aec.Mcp.Core.Operations.Modify;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Modify.ElementMove;

internal sealed class RevitElementMoveExecutor
{
    public McpToolResult Execute(RevitToolContext context, ElementMovePlan plan)
    {
        foreach (var item in plan.Items)
            ElementTransformUtils.MoveElement(context.Document, RevitIds.Id(item.ElementId), ToXyz(item.Offset));

        var ids = plan.Items.Select(item => item.ElementId).ToList();
        return ToolResults.Success(new { count = ids.Count, elementIds = ids }, $"{ids.Count} element(s) moved.");
    }

    private static XYZ ToXyz(AecPoint3 point)
        => new(point.X, point.Y, point.Z);
}
