using System.Text.Json;
using Algomim.Aec.Mcp.Core.Operations.Create;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Composition;

namespace Algomim.Revit.Mcp.Tools.Create.ViewSectionCreate;

internal sealed class ViewSectionCreateTool : RevitPlannedWriteToolBase<ViewSectionCreatePlan>
{
    private readonly RevitViewSectionCreateExecutor _executor;

    private ViewSectionCreateTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore, RevitViewSectionCreateExecutor executor)
        : base(dispatcher, documentContextStore) => _executor = executor;

    public override string Name => "create_view_sections";
    public override string Description => "Create section or detail views from base line, depth and height.";
    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new
        {
            names = ArrayOf("string"),
            startX = ArrayOf("number"),
            startY = ArrayOf("number"),
            startZ = ArrayOf("number"),
            endX = ArrayOf("number"),
            endY = ArrayOf("number"),
            endZ = ArrayOf("number"),
            depths = ArrayOf("number"),
            heights = ArrayOf("number"),
            detailViews = ArrayOf("boolean"),
        },
        required = new[] { "names", "startX", "startY", "startZ", "endX", "endY", "endZ", "depths", "heights", "detailViews" },
    });
    public override ToolMetadata Metadata { get; } = new("create_view_sections", ToolCategory.Create, ToolMode.Write, ToolRisk.High, "Create section or detail views from base line, depth and height.");

    public static IMcpTool Create(RevitToolServices services)
        => new ViewSectionCreateTool(services.Dispatcher, services.DocumentContextStore, new RevitViewSectionCreateExecutor());

    protected override ViewSectionCreatePlan CreatePlan(ArgumentReader arguments)
    {
        var args = ViewSectionCreateArgs.From(arguments);
        return ViewSectionCreatePlanner.CreatePlan(args.Names, args.StartX, args.StartY, args.StartZ, args.EndX, args.EndY, args.EndZ, args.Depths, args.Heights, args.DetailViews);
    }

    protected override McpToolResult ExecutePlan(RevitToolContext context, ViewSectionCreatePlan plan)
        => _executor.Execute(context, plan);

    private static object ArrayOf(string type)
        => new { type = "array", items = new { type } };
}
