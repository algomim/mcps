using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Algomim.Aec.Mcp.Core.Operations.Create;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Create.RoomElevationCreate;

internal sealed class RevitRoomElevationCreateExecutor
{
    public McpToolResult Execute(RevitToolContext context, RoomElevationCreatePlan plan)
    {
        var type = CreateRevitHelpers.FindViewFamilyType(context.Document, ViewFamily.Elevation);
        var created = new List<object>();

        foreach (var item in plan.Rooms)
        {
            if (context.Document.GetElement(RevitIds.Id(item.RoomId)) is not Room room || room.Location is not LocationPoint location)
                continue;

            var marker = ElevationMarker.CreateElevationMarker(context.Document, type.Id, location.Point, item.Scale);
            var includes = new[] { item.IncludeEast, item.IncludeNorth, item.IncludeWest, item.IncludeSouth };
            for (var index = 0; index < includes.Length; index++)
            {
                if (!includes[index]) continue;
                var view = marker.CreateElevation(context.Document, RevitIds.Id(item.ViewPlanId), index);
                view.Name = $"{room.Name} Elevation {index + 1}";
                created.Add(RevitElementSummary.FromElement(view));
            }
        }

        return ToolResults.Success(new { count = created.Count, views = created }, $"{created.Count} room elevation view(s) created.");
    }
}
