using Autodesk.Revit.DB;
using Algomim.Aec.Mcp.Core.Operations.Create;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Create.ViewSectionCreate;

internal sealed class RevitViewSectionCreateExecutor
{
    public McpToolResult Execute(RevitToolContext context, ViewSectionCreatePlan plan)
    {
        var created = new List<object>();
        foreach (var item in plan.Views)
        {
            var family = item.IsDetailView ? ViewFamily.Detail : ViewFamily.Section;
            var type = CreateRevitHelpers.FindViewFamilyType(context.Document, family);
            var box = CreateRevitHelpers.SectionBox(item.Start, item.End, item.Depth, item.Height);
            var view = item.IsDetailView
                ? ViewSection.CreateDetail(context.Document, type.Id, box)
                : ViewSection.CreateSection(context.Document, type.Id, box);
            view.Name = item.Name;
            created.Add(RevitElementSummary.FromElement(view));
        }

        return ToolResults.Success(new { count = created.Count, views = created }, $"{created.Count} section/detail view(s) created.");
    }
}
