using System.Text.Json;
using Algomim.Aec.Mcp.Core.Operations.Create;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Composition;

namespace Algomim.Revit.Mcp.Tools.Create.ViewPlanCreate;

internal sealed class ViewPlanCreateTool : RevitPlannedWriteToolBase<ViewPlanCreatePlan>
{
    private readonly RevitViewPlanCreateExecutor _executor;

    private ViewPlanCreateTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore, RevitViewPlanCreateExecutor executor)
        : base(dispatcher, documentContextStore) => _executor = executor;

    public override string Name => "view_create_plans";
    public override string Description => "Create floor or ceiling plan views from level ids.";
    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new { names = ArrayOf("string"), levelIds = ArrayOf("integer"), ceilingPlans = ArrayOf("boolean") },
        required = new[] { "names", "levelIds", "ceilingPlans" },
    });
    public override ToolMetadata Metadata { get; } = new("view_create_plans", ToolCategory.Create, ToolMode.Write, ToolRisk.High, "Create floor or ceiling plan views from level ids.");

    public static IMcpTool Create(RevitToolServices services)
        => new ViewPlanCreateTool(services.Dispatcher, services.DocumentContextStore, new RevitViewPlanCreateExecutor());

    protected override ViewPlanCreatePlan CreatePlan(ArgumentReader arguments)
    {
        var args = ViewPlanCreateArgs.From(arguments);
        return ViewPlanCreatePlanner.CreatePlan(args.Names, args.LevelIds, args.CeilingPlans);
    }

    protected override McpToolResult ExecutePlan(RevitToolContext context, ViewPlanCreatePlan plan)
        => _executor.Execute(context, plan);

    private static object ArrayOf(string type)
        => new { type = "array", items = new { type } };
}
