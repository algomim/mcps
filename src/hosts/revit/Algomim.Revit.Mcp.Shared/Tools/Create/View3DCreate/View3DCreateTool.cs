using System.Text.Json;
using Algomim.Aec.Mcp.Core.Operations.Create;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Composition;

namespace Algomim.Revit.Mcp.Tools.Create.View3DCreate;

internal sealed class View3DCreateTool : RevitPlannedWriteToolBase<View3DCreatePlan>
{
    private readonly RevitView3DCreateExecutor _executor;

    private View3DCreateTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore, RevitView3DCreateExecutor executor)
        : base(dispatcher, documentContextStore) => _executor = executor;

    public override string Name => "create_view_3ds";
    public override string Description => "Create isometric 3D views.";
    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new { names = ArrayOf("string") },
        required = new[] { "names" },
    });
    public override ToolMetadata Metadata { get; } = new("create_view_3ds", ToolCategory.Create, ToolMode.Write, ToolRisk.High, "Create isometric 3D views.");

    public static IMcpTool Create(RevitToolServices services)
        => new View3DCreateTool(services.Dispatcher, services.DocumentContextStore, new RevitView3DCreateExecutor());

    protected override View3DCreatePlan CreatePlan(ArgumentReader arguments)
    {
        var args = View3DCreateArgs.From(arguments);
        return View3DCreatePlanner.CreatePlan(args.Names);
    }

    protected override McpToolResult ExecutePlan(RevitToolContext context, View3DCreatePlan plan)
        => _executor.Execute(context, plan);

    private static object ArrayOf(string type)
        => new { type = "array", items = new { type } };
}
