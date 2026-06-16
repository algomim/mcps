using Autodesk.Revit.DB;
using Algomim.Aec.Mcp.Core.Operations.Create;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Create.View3DCreate;

internal sealed class RevitView3DCreateExecutor
{
    public McpToolResult Execute(RevitToolContext context, View3DCreatePlan plan)
    {
        var type = CreateRevitHelpers.FindViewFamilyType(context.Document, ViewFamily.ThreeDimensional);
        var views = plan.Names.Select(name =>
        {
            var view = View3D.CreateIsometric(context.Document, type.Id);
            view.Name = name;
            return RevitElementSummary.FromElement(view);
        }).ToList();

        return ToolResults.Success(new { count = views.Count, views }, $"{views.Count} 3D view(s) created.");
    }
}
