using System.Text.Json;
using Algomim.Aec.Mcp.Core.Operations.Create;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Composition;

namespace Algomim.Revit.Mcp.Tools.Create.DraftingOrLegendViewCreate;

internal sealed class DraftingOrLegendViewCreateTool : RevitPlannedWriteToolBase<DraftingOrLegendViewCreatePlan>
{
    private readonly RevitDraftingOrLegendViewCreateExecutor _executor;

    private DraftingOrLegendViewCreateTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore, RevitDraftingOrLegendViewCreateExecutor executor)
        : base(dispatcher, documentContextStore) => _executor = executor;

    public override string Name => "create_drafting_or_legend_views";
    public override string Description => "Create drafting views or duplicate an existing legend view.";
    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new { names = ArrayOf("string"), draftingViews = ArrayOf("boolean") },
        required = new[] { "names", "draftingViews" },
    });
    public override ToolMetadata Metadata { get; } = new("create_drafting_or_legend_views", ToolCategory.Create, ToolMode.Write, ToolRisk.High, "Create drafting views or duplicate an existing legend view.");

    public static IMcpTool Create(RevitToolServices services)
        => new DraftingOrLegendViewCreateTool(services.Dispatcher, services.DocumentContextStore, new RevitDraftingOrLegendViewCreateExecutor());

    protected override DraftingOrLegendViewCreatePlan CreatePlan(ArgumentReader arguments)
    {
        var args = DraftingOrLegendViewCreateArgs.From(arguments);
        return DraftingOrLegendViewCreatePlanner.CreatePlan(args.Names, args.DraftingViews);
    }

    protected override McpToolResult ExecutePlan(RevitToolContext context, DraftingOrLegendViewCreatePlan plan)
        => _executor.Execute(context, plan);

    private static object ArrayOf(string type)
        => new { type = "array", items = new { type } };
}
