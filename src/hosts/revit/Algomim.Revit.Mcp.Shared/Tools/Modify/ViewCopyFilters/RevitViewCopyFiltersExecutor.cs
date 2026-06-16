using Autodesk.Revit.DB;
using Algomim.Aec.Mcp.Core.Operations.Modify;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;
using RevitView = Autodesk.Revit.DB.View;

namespace Algomim.Revit.Mcp.Tools.Modify.ViewCopyFilters;

internal sealed class RevitViewCopyFiltersExecutor
{
    public McpToolResult Execute(RevitToolContext context, ViewCopyFiltersPlan plan)
    {
        var sourceView = context.Document.GetElement(RevitIds.Id(plan.SourceViewId)) as RevitView;
        if (sourceView is null)
            return ToolResults.Error("VIEW_NOT_FOUND", "Source view not found.");

        var filterIds = RevitIds.ToElementIds(plan.FilterIds);
        var results = new List<object>();
        foreach (var targetId in plan.TargetViewIds)
        {
            var targetView = context.Document.GetElement(RevitIds.Id(targetId)) as RevitView;
            if (targetView is null)
            {
                results.Add(new { viewId = targetId, ok = false, error = "Target view not found." });
                continue;
            }

            foreach (var filterId in filterIds)
            {
                if (!targetView.GetFilters().Contains(filterId))
                    targetView.AddFilter(filterId);

                targetView.SetFilterOverrides(filterId, sourceView.GetFilterOverrides(filterId));
                targetView.SetFilterVisibility(filterId, sourceView.GetFilterVisibility(filterId));
            }

            results.Add(new { viewId = targetId, ok = true, filterCount = filterIds.Count });
        }

        return ToolResults.Success(new { count = results.Count, results }, $"{results.Count} target view(s) updated.");
    }
}
