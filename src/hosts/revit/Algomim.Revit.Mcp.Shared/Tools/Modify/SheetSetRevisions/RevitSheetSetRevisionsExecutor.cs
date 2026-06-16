using Autodesk.Revit.DB;
using Algomim.Aec.Mcp.Core.Operations.Modify;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Modify.SheetSetRevisions;

internal sealed class RevitSheetSetRevisionsExecutor
{
    public McpToolResult Execute(RevitToolContext context, SheetSetRevisionsPlan plan)
    {
        var revisionIds = RevitIds.ToElementIds(plan.RevisionIds);
        var results = new List<object>();
        foreach (var sheetId in plan.SheetIds)
        {
            if (context.Document.GetElement(RevitIds.Id(sheetId)) is not ViewSheet sheet)
            {
                results.Add(new { sheetId, ok = false, error = "Sheet not found." });
                continue;
            }

            var current = sheet.GetAdditionalRevisionIds().ToHashSet();
            foreach (var revisionId in revisionIds)
            {
                if (plan.Assign) current.Add(revisionId);
                else current.Remove(revisionId);
            }

            sheet.SetAdditionalRevisionIds(current.ToList());
            results.Add(new { sheetId, ok = true, revisionCount = current.Count });
        }

        return ToolResults.Success(new { count = results.Count, results }, $"{results.Count} sheet revision operation(s).");
    }
}
