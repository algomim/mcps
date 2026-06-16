using Autodesk.Revit.DB;
using Algomim.Aec.Mcp.Core.Operations.Create;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;
using RevitView = Autodesk.Revit.DB.View;

namespace Algomim.Revit.Mcp.Tools.Create.DraftingOrLegendViewCreate;

internal sealed class RevitDraftingOrLegendViewCreateExecutor
{
    public McpToolResult Execute(RevitToolContext context, DraftingOrLegendViewCreatePlan plan)
    {
        var draftingType = CreateRevitHelpers.FindViewFamilyType(context.Document, ViewFamily.Drafting);
        var firstLegend = new FilteredElementCollector(context.Document)
            .OfClass(typeof(RevitView))
            .Cast<RevitView>()
            .FirstOrDefault(view => view.ViewType == ViewType.Legend);
        var created = new List<object>();

        foreach (var item in plan.Views)
        {
            RevitView view;
            if (item.IsDraftingView)
            {
                view = ViewDrafting.Create(context.Document, draftingType.Id);
            }
            else if (firstLegend is not null)
            {
                view = (RevitView)context.Document.GetElement(firstLegend.Duplicate(ViewDuplicateOption.Duplicate));
            }
            else
            {
                return ToolResults.Error("NO_LEGEND_TEMPLATE", "No existing legend view is available to duplicate.");
            }

            view.Name = item.Name;
            created.Add(RevitElementSummary.FromElement(view));
        }

        return ToolResults.Success(new { count = created.Count, views = created }, $"{created.Count} drafting/legend view(s) created.");
    }
}
