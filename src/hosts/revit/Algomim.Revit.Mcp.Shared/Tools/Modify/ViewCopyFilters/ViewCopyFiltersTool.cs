using System.Text.Json;
using Algomim.Aec.Mcp.Core.Operations.Modify;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Composition;

namespace Algomim.Revit.Mcp.Tools.Modify.ViewCopyFilters;

internal sealed class ViewCopyFiltersTool : RevitPlannedWriteToolBase<ViewCopyFiltersPlan>
{
    private readonly RevitViewCopyFiltersExecutor _executor;

    private ViewCopyFiltersTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore, RevitViewCopyFiltersExecutor executor)
        : base(dispatcher, documentContextStore) => _executor = executor;

    public override string Name => "view_copy_filters";
    public override string Description => "Copy selected view filters and overrides from one view to target views.";
    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new { sourceViewId = new { type = "integer" }, filterIds = ArrayOf("integer"), targetViewIds = ArrayOf("integer") },
        required = new[] { "sourceViewId", "filterIds", "targetViewIds" },
    });
    public override ToolMetadata Metadata { get; } = new("view_copy_filters", ToolCategory.Graphics, ToolMode.Write, ToolRisk.High, "Copy selected view filters and overrides from one view to target views.");

    public static IMcpTool Create(RevitToolServices services)
        => new ViewCopyFiltersTool(services.Dispatcher, services.DocumentContextStore, new RevitViewCopyFiltersExecutor());

    protected override ViewCopyFiltersPlan CreatePlan(ArgumentReader arguments)
    {
        var args = ViewCopyFiltersArgs.From(arguments);
        return ViewCopyFiltersPlanner.CreatePlan(args.SourceViewId, args.FilterIds, args.TargetViewIds);
    }

    protected override McpToolResult ExecutePlan(RevitToolContext context, ViewCopyFiltersPlan plan)
        => _executor.Execute(context, plan);

    private static object ArrayOf(string type)
        => new { type = "array", items = new { type } };
}
