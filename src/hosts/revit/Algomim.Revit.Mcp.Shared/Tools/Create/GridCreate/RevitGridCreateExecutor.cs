using Autodesk.Revit.DB;
using Algomim.Aec.Mcp.Core.Geometry;
using Algomim.Aec.Mcp.Core.Operations.Create;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Create.GridCreate;

internal sealed class RevitGridCreateExecutor
{
    public McpToolResult Execute(RevitToolContext context, GridCreatePlan plan)
    {
        var created = new List<object>();
        foreach (var line in plan.Lines)
        {
            var axis = Line.CreateBound(
                ToXyz(line.Start),
                ToXyz(line.End));
            var grid = Grid.Create(context.Document, axis);
            grid.Name = line.Name;
            created.Add(RevitElementSummary.FromElement(grid));
        }

        return ToolResults.Success(new { count = created.Count, grids = created }, $"{created.Count} grid(s) created.");
    }

    private static XYZ ToXyz(AecPoint3 point)
        => new(point.X, point.Y, point.Z);
}
