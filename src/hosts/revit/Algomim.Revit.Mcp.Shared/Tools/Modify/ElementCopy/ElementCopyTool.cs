using System.Text.Json;
using Algomim.Aec.Mcp.Core.Operations.Modify;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Composition;

namespace Algomim.Revit.Mcp.Tools.Modify.ElementCopy;

internal sealed class ElementCopyTool : RevitPlannedWriteToolBase<ElementCopyPlan>
{
    private readonly RevitElementCopyExecutor _executor;

    private ElementCopyTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore, RevitElementCopyExecutor executor)
        : base(dispatcher, documentContextStore) => _executor = executor;

    public override string Name => "element_copy";
    public override string Description => "Copy element ids by XYZ vectors in Revit internal feet.";
    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new { elementIds = ArrayOf("integer"), x = ArrayOf("number"), y = ArrayOf("number"), z = ArrayOf("number") },
        required = new[] { "elementIds", "x", "y", "z" },
    });
    public override ToolMetadata Metadata { get; } = new("element_copy", ToolCategory.Modify, ToolMode.Write, ToolRisk.High, "Copy element ids by XYZ vectors in Revit internal feet.");

    public static IMcpTool Create(RevitToolServices services)
        => new ElementCopyTool(services.Dispatcher, services.DocumentContextStore, new RevitElementCopyExecutor());

    protected override ElementCopyPlan CreatePlan(ArgumentReader arguments)
    {
        var args = ElementCopyArgs.From(arguments);
        return ElementCopyPlanner.CreatePlan(args.ElementIds, args.X, args.Y, args.Z);
    }

    protected override McpToolResult ExecutePlan(RevitToolContext context, ElementCopyPlan plan)
        => _executor.Execute(context, plan);

    private static object ArrayOf(string type)
        => new { type = "array", items = new { type } };
}
