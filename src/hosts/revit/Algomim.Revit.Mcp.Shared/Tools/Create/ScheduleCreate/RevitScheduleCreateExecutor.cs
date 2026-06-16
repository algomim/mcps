using Autodesk.Revit.DB;
using Algomim.Aec.Mcp.Core.Operations.Create;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Create.ScheduleCreate;

internal sealed class RevitScheduleCreateExecutor
{
    public McpToolResult Execute(RevitToolContext context, ScheduleCreatePlan plan)
    {
        var schedule = ViewSchedule.CreateSchedule(context.Document, RevitIds.Id(plan.CategoryId));
        schedule.Name = plan.Name;
        var definition = schedule.Definition;
        var schedulableFields = definition.GetSchedulableFields();
        foreach (var parameterId in plan.ParameterIds.Select(RevitIds.Id))
        {
            var field = schedulableFields.FirstOrDefault(candidate => candidate.ParameterId == parameterId);
            if (field is not null)
                definition.AddField(field);
        }

        return ToolResults.Success(RevitElementSummary.FromElement(schedule), "Schedule created.");
    }
}
