using System.Text.Json;
using Algomim.Aec.Mcp.Core.Operations.Create;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Composition;

namespace Algomim.Revit.Mcp.Tools.Create.RoomElevationCreate;

internal sealed class RoomElevationCreateTool : RevitPlannedWriteToolBase<RoomElevationCreatePlan>
{
    private readonly RevitRoomElevationCreateExecutor _executor;

    private RoomElevationCreateTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore, RevitRoomElevationCreateExecutor executor)
        : base(dispatcher, documentContextStore) => _executor = executor;

    public override string Name => "view_create_room_elevations";
    public override string Description => "Create room elevation views using elevation markers.";
    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new
        {
            roomIds = ArrayOf("integer"),
            viewPlanIds = ArrayOf("integer"),
            scales = ArrayOf("integer"),
            includeNorth = ArrayOf("boolean"),
            includeWest = ArrayOf("boolean"),
            includeSouth = ArrayOf("boolean"),
            includeEast = ArrayOf("boolean"),
        },
        required = new[] { "roomIds", "viewPlanIds", "scales", "includeNorth", "includeWest", "includeSouth", "includeEast" },
    });
    public override ToolMetadata Metadata { get; } = new("view_create_room_elevations", ToolCategory.Create, ToolMode.Write, ToolRisk.High, "Create room elevation views using elevation markers.");

    public static IMcpTool Create(RevitToolServices services)
        => new RoomElevationCreateTool(services.Dispatcher, services.DocumentContextStore, new RevitRoomElevationCreateExecutor());

    protected override RoomElevationCreatePlan CreatePlan(ArgumentReader arguments)
    {
        var args = RoomElevationCreateArgs.From(arguments);
        return RoomElevationCreatePlanner.CreatePlan(args.RoomIds, args.ViewPlanIds, args.Scales, args.IncludeNorth, args.IncludeWest, args.IncludeSouth, args.IncludeEast);
    }

    protected override McpToolResult ExecutePlan(RevitToolContext context, RoomElevationCreatePlan plan)
        => _executor.Execute(context, plan);

    private static object ArrayOf(string type)
        => new { type = "array", items = new { type } };
}
