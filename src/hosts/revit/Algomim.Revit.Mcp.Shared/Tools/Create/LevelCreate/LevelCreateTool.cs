using System.Text.Json;
using Algomim.Aec.Mcp.Core.Operations.Create;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Composition;

namespace Algomim.Revit.Mcp.Tools.Create.LevelCreate;

internal sealed class LevelCreateTool : RevitPlannedWriteToolBase<LevelCreatePlan>
{
    private readonly RevitLevelCreateExecutor _executor;

    private LevelCreateTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore, RevitLevelCreateExecutor executor)
        : base(dispatcher, documentContextStore) => _executor = executor;

    public override string Name => "level_create";
    public override string Description => "Create levels from names and elevations in feet.";
    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new { names = ArrayOf("string"), elevations = ArrayOf("number") },
        required = new[] { "names", "elevations" },
    });
    public override ToolMetadata Metadata { get; } = new("level_create", ToolCategory.Create, ToolMode.Write, ToolRisk.High, "Create levels from names and elevations in feet.");

    public static IMcpTool Create(RevitToolServices services)
        => new LevelCreateTool(services.Dispatcher, services.DocumentContextStore, new RevitLevelCreateExecutor());

    protected override LevelCreatePlan CreatePlan(ArgumentReader arguments)
    {
        var args = LevelCreateArgs.From(arguments);
        return LevelCreatePlanner.CreatePlan(args.Names, args.Elevations);
    }

    protected override McpToolResult ExecutePlan(RevitToolContext context, LevelCreatePlan plan)
        => _executor.Execute(context, plan);

    private static object ArrayOf(string type)
        => new { type = "array", items = new { type } };
}
