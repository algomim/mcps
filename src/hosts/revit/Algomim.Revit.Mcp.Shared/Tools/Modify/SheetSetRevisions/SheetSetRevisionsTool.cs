using System.Text.Json;
using Algomim.Aec.Mcp.Core.Operations.Modify;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Composition;

namespace Algomim.Revit.Mcp.Tools.Modify.SheetSetRevisions;

internal sealed class SheetSetRevisionsTool : RevitPlannedWriteToolBase<SheetSetRevisionsPlan>
{
    private readonly RevitSheetSetRevisionsExecutor _executor;

    private SheetSetRevisionsTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore, RevitSheetSetRevisionsExecutor executor)
        : base(dispatcher, documentContextStore) => _executor = executor;

    public override string Name => "sheet_set_revisions";
    public override string Description => "Assign or unassign revision ids on sheet ids.";
    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new { sheetIds = ArrayOf("integer"), revisionIds = ArrayOf("integer"), assign = new { type = "boolean" } },
        required = new[] { "sheetIds", "revisionIds", "assign" },
    });
    public override ToolMetadata Metadata { get; } = new("sheet_set_revisions", ToolCategory.Sheet, ToolMode.Write, ToolRisk.High, "Assign or unassign revision ids on sheet ids.");

    public static IMcpTool Create(RevitToolServices services)
        => new SheetSetRevisionsTool(services.Dispatcher, services.DocumentContextStore, new RevitSheetSetRevisionsExecutor());

    protected override SheetSetRevisionsPlan CreatePlan(ArgumentReader arguments)
    {
        var args = SheetSetRevisionsArgs.From(arguments);
        return SheetSetRevisionsPlanner.CreatePlan(args.SheetIds, args.RevisionIds, args.Assign);
    }

    protected override McpToolResult ExecutePlan(RevitToolContext context, SheetSetRevisionsPlan plan)
        => _executor.Execute(context, plan);

    private static object ArrayOf(string type)
        => new { type = "array", items = new { type } };
}
