using System.Text.Json;
using Algomim.Aec.Mcp.Core.Operations.Create;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Composition;

namespace Algomim.Revit.Mcp.Tools.Create.TagCreate;

internal sealed class TagCreateTool : RevitPlannedWriteToolBase<TagCreatePlan>
{
    private readonly RevitTagCreateExecutor _executor;

    private TagCreateTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore, RevitTagCreateExecutor executor)
        : base(dispatcher, documentContextStore) => _executor = executor;

    public override string Name => "tag_create";
    public override string Description => "Create category tags for element ids in a view.";
    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new
        {
            elementIds = ArrayOf("integer"),
            viewId = new { type = "integer" },
            offsetX = new { type = "number" },
            offsetY = new { type = "number" },
            addLeader = new { type = "boolean" },
            addElbowHorizontalLeader = new { type = "boolean" },
        },
        required = new[] { "elementIds", "viewId", "offsetX", "offsetY", "addLeader", "addElbowHorizontalLeader" },
    });
    public override ToolMetadata Metadata { get; } = new("tag_create", ToolCategory.Create, ToolMode.Write, ToolRisk.High, "Create category tags for element ids in a view.");

    public static IMcpTool Create(RevitToolServices services)
        => new TagCreateTool(services.Dispatcher, services.DocumentContextStore, new RevitTagCreateExecutor());

    protected override TagCreatePlan CreatePlan(ArgumentReader arguments)
    {
        var args = TagCreateArgs.From(arguments);
        return TagCreatePlanner.CreatePlan(args.ElementIds, args.ViewId, args.OffsetX, args.OffsetY, args.AddLeader, args.AddElbowHorizontalLeader);
    }

    protected override McpToolResult ExecutePlan(RevitToolContext context, TagCreatePlan plan)
        => _executor.Execute(context, plan);

    private static object ArrayOf(string type)
        => new { type = "array", items = new { type } };
}
