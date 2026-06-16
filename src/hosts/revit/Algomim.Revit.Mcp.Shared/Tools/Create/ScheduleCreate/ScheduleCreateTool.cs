using System.Text.Json;
using Algomim.Aec.Mcp.Core.Operations.Create;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Composition;

namespace Algomim.Revit.Mcp.Tools.Create.ScheduleCreate;

internal sealed class ScheduleCreateTool : RevitPlannedWriteToolBase<ScheduleCreatePlan>
{
    private readonly RevitScheduleCreateExecutor _executor;

    private ScheduleCreateTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore, RevitScheduleCreateExecutor executor)
        : base(dispatcher, documentContextStore) => _executor = executor;

    public override string Name => "schedule_create";
    public override string Description => "Create a schedule for a category and add parameter columns.";
    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new { name = new { type = "string" }, categoryId = new { type = "integer" }, parameterIds = ArrayOf("integer") },
        required = new[] { "name", "categoryId", "parameterIds" },
    });
    public override ToolMetadata Metadata { get; } = new("schedule_create", ToolCategory.Create, ToolMode.Write, ToolRisk.High, "Create a schedule for a category and add parameter columns.");

    public static IMcpTool Create(RevitToolServices services)
        => new ScheduleCreateTool(services.Dispatcher, services.DocumentContextStore, new RevitScheduleCreateExecutor());

    protected override ScheduleCreatePlan CreatePlan(ArgumentReader arguments)
    {
        var args = ScheduleCreateArgs.From(arguments);
        return ScheduleCreatePlanner.CreatePlan(args.Name, args.CategoryId, args.ParameterIds);
    }

    protected override McpToolResult ExecutePlan(RevitToolContext context, ScheduleCreatePlan plan)
        => _executor.Execute(context, plan);

    private static object ArrayOf(string type)
        => new { type = "array", items = new { type } };
}
