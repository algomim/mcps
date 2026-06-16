using System.Text.Json;
using Algomim.Aec.Mcp.Core.Operations.Modify;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Composition;

namespace Algomim.Revit.Mcp.Tools.Modify.ElementMove;

internal sealed class ElementMoveTool : RevitPlannedWriteToolBase<ElementMovePlan>
{
    private readonly RevitElementMoveExecutor _executor;

    private ElementMoveTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore, RevitElementMoveExecutor executor)
        : base(dispatcher, documentContextStore) => _executor = executor;

    public override string Name => "element_move";
    public override string Description => "Move element ids by XYZ vectors in Revit internal feet.";
    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new { elementIds = ArrayOf("integer"), x = ArrayOf("number"), y = ArrayOf("number"), z = ArrayOf("number") },
        required = new[] { "elementIds", "x", "y", "z" },
    });
    public override ToolMetadata Metadata { get; } = new("element_move", ToolCategory.Modify, ToolMode.Write, ToolRisk.High, "Move element ids by XYZ vectors in Revit internal feet.");

    public static IMcpTool Create(RevitToolServices services)
        => new ElementMoveTool(services.Dispatcher, services.DocumentContextStore, new RevitElementMoveExecutor());

    protected override ElementMovePlan CreatePlan(ArgumentReader arguments)
    {
        var args = ElementMoveArgs.From(arguments);
        return ElementMovePlanner.CreatePlan(args.ElementIds, args.X, args.Y, args.Z);
    }

    protected override McpToolResult ExecutePlan(RevitToolContext context, ElementMovePlan plan)
        => _executor.Execute(context, plan);

    private static object ArrayOf(string type)
        => new { type = "array", items = new { type } };
}
