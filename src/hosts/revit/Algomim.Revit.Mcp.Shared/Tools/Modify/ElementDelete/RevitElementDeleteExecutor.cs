using Algomim.Aec.Mcp.Core.Operations.Modify;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Modify.ElementDelete;

internal sealed class RevitElementDeleteExecutor
{
    public McpToolResult Execute(RevitToolContext context, ElementDeletePlan plan)
    {
        var ids = RevitIds.ToElementIds(plan.ElementIds);
        var deleted = context.Document.Delete(ids).Select(id => id.Value).ToList();
        return ToolResults.Success(new { requestedCount = ids.Count, deletedCount = deleted.Count, deletedElementIds = deleted }, $"{deleted.Count} element(s) deleted.");
    }
}
