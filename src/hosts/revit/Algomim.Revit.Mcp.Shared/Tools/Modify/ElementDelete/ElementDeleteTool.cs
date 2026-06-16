using System.Text.Json;
using Algomim.Aec.Mcp.Core.Operations.Modify;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Composition;

namespace Algomim.Revit.Mcp.Tools.Modify.ElementDelete;

internal sealed class ElementDeleteTool : RevitPlannedWriteToolBase<ElementDeletePlan>
{
    private readonly RevitElementDeleteExecutor _executor;

    private ElementDeleteTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore, RevitElementDeleteExecutor executor)
        : base(dispatcher, documentContextStore) => _executor = executor;

    public override string Name => "element_delete";
    public override string Description => "Delete element ids from the document.";
    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new { elementIds = ArrayOf("integer") },
        required = new[] { "elementIds" },
    });
    public override ToolMetadata Metadata { get; } = new("element_delete", ToolCategory.Modify, ToolMode.Write, ToolRisk.Destructive, "Delete element ids from the document.");

    public static IMcpTool Create(RevitToolServices services)
        => new ElementDeleteTool(services.Dispatcher, services.DocumentContextStore, new RevitElementDeleteExecutor());

    protected override ElementDeletePlan CreatePlan(ArgumentReader arguments)
    {
        var args = ElementDeleteArgs.From(arguments);
        return ElementDeletePlanner.CreatePlan(args.ElementIds);
    }

    protected override McpToolResult ExecutePlan(RevitToolContext context, ElementDeletePlan plan)
        => _executor.Execute(context, plan);

    private static object ArrayOf(string type)
        => new { type = "array", items = new { type } };
}
