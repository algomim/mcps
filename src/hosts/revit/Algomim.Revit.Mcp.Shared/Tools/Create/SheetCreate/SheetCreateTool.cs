using System.Text.Json;
using Algomim.Aec.Mcp.Core.Operations.Create;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Composition;

namespace Algomim.Revit.Mcp.Tools.Create.SheetCreate;

internal sealed class SheetCreateTool : RevitPlannedWriteToolBase<SheetCreatePlan>
{
    private readonly RevitSheetCreateExecutor _executor;

    private SheetCreateTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore, RevitSheetCreateExecutor executor)
        : base(dispatcher, documentContextStore) => _executor = executor;

    public override string Name => "create_sheets";
    public override string Description => "Create sheets with optional titleblock type ids.";
    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new { names = ArrayOf("string"), titleblockTypeIds = ArrayOf("integer") },
        required = new[] { "names" },
    });
    public override ToolMetadata Metadata { get; } = new("create_sheets", ToolCategory.Create, ToolMode.Write, ToolRisk.High, "Create sheets with optional titleblock type ids.");

    public static IMcpTool Create(RevitToolServices services)
        => new SheetCreateTool(services.Dispatcher, services.DocumentContextStore, new RevitSheetCreateExecutor());

    protected override SheetCreatePlan CreatePlan(ArgumentReader arguments)
    {
        var args = SheetCreateArgs.From(arguments);
        return SheetCreatePlanner.CreatePlan(args.Names, args.TitleblockTypeIds);
    }

    protected override McpToolResult ExecutePlan(RevitToolContext context, SheetCreatePlan plan)
        => _executor.Execute(context, plan);

    private static object ArrayOf(string type)
        => new { type = "array", items = new { type } };
}
