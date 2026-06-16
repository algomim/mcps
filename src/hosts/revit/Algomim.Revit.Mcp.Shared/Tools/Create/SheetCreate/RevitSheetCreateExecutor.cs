using Autodesk.Revit.DB;
using Algomim.Aec.Mcp.Core.Operations.Create;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Create.SheetCreate;

internal sealed class RevitSheetCreateExecutor
{
    public McpToolResult Execute(RevitToolContext context, SheetCreatePlan plan)
    {
        var created = new List<object>();
        foreach (var item in plan.Sheets)
        {
            var titleblockId = item.TitleblockTypeId.HasValue ? RevitIds.Id(item.TitleblockTypeId.Value) : ElementId.InvalidElementId;
            var sheet = ViewSheet.Create(context.Document, titleblockId);
            sheet.Name = item.Name;
            created.Add(RevitElementSummary.FromElement(sheet));
        }

        return ToolResults.Success(new { count = created.Count, sheets = created }, $"{created.Count} sheet(s) created.");
    }
}
