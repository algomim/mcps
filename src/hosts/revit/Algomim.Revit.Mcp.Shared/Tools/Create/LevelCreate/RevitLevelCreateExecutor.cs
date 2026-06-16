using Autodesk.Revit.DB;
using Algomim.Aec.Mcp.Core.Operations.Create;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Create.LevelCreate;

internal sealed class RevitLevelCreateExecutor
{
    public McpToolResult Execute(RevitToolContext context, LevelCreatePlan plan)
    {
        var created = new List<object>();
        foreach (var item in plan.Levels)
        {
            var level = Level.Create(context.Document, item.Elevation);
            level.Name = item.Name;
            created.Add(RevitElementSummary.FromElement(level));
        }

        return ToolResults.Success(new { count = created.Count, levels = created }, $"{created.Count} level(s) created.");
    }
}
