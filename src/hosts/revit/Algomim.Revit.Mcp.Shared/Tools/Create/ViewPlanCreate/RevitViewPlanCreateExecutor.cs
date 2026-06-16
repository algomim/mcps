using Autodesk.Revit.DB;
using Algomim.Aec.Mcp.Core.Operations.Create;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;
using RevitDocument = Autodesk.Revit.DB.Document;

namespace Algomim.Revit.Mcp.Tools.Create.ViewPlanCreate;

internal sealed class RevitViewPlanCreateExecutor
{
    public McpToolResult Execute(RevitToolContext context, ViewPlanCreatePlan plan)
    {
        var created = new List<object>();
        foreach (var item in plan.Views)
        {
            var family = item.IsCeilingPlan ? ViewFamily.CeilingPlan : ViewFamily.FloorPlan;
            var type = FindViewFamilyType(context.Document, family);
            var view = ViewPlan.Create(context.Document, type.Id, RevitIds.Id(item.LevelId));
            view.Name = item.Name;
            created.Add(RevitElementSummary.FromElement(view));
        }

        return ToolResults.Success(new { count = created.Count, views = created }, $"{created.Count} plan view(s) created.");
    }

    private static ViewFamilyType FindViewFamilyType(RevitDocument document, ViewFamily family)
        => new FilteredElementCollector(document)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .First(type => type.ViewFamily == family);
}
