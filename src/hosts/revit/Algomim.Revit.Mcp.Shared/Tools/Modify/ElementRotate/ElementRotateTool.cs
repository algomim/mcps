using System.Text.Json;
using Algomim.Aec.Mcp.Core.Operations.Modify;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Composition;

namespace Algomim.Revit.Mcp.Tools.Modify.ElementRotate;

internal sealed class ElementRotateTool : RevitPlannedWriteToolBase<ElementRotatePlan>
{
    private readonly RevitElementRotateExecutor _executor;

    private ElementRotateTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore, RevitElementRotateExecutor executor)
        : base(dispatcher, documentContextStore) => _executor = executor;

    public override string Name => "element_rotate";
    public override string Description => "Rotate element ids around axes by radians.";
    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new
        {
            elementIds = ArrayOf("integer"),
            axisStartX = ArrayOf("number"),
            axisStartY = ArrayOf("number"),
            axisStartZ = ArrayOf("number"),
            axisEndX = ArrayOf("number"),
            axisEndY = ArrayOf("number"),
            axisEndZ = ArrayOf("number"),
            angles = ArrayOf("number"),
        },
        required = new[] { "elementIds", "axisStartX", "axisStartY", "axisStartZ", "axisEndX", "axisEndY", "axisEndZ", "angles" },
    });
    public override ToolMetadata Metadata { get; } = new("element_rotate", ToolCategory.Modify, ToolMode.Write, ToolRisk.High, "Rotate element ids around axes by radians.");

    public static IMcpTool Create(RevitToolServices services)
        => new ElementRotateTool(services.Dispatcher, services.DocumentContextStore, new RevitElementRotateExecutor());

    protected override ElementRotatePlan CreatePlan(ArgumentReader arguments)
    {
        var args = ElementRotateArgs.From(arguments);
        return ElementRotatePlanner.CreatePlan(args.ElementIds, args.AxisStartX, args.AxisStartY, args.AxisStartZ, args.AxisEndX, args.AxisEndY, args.AxisEndZ, args.Angles);
    }

    protected override McpToolResult ExecutePlan(RevitToolContext context, ElementRotatePlan plan)
        => _executor.Execute(context, plan);

    private static object ArrayOf(string type)
        => new { type = "array", items = new { type } };
}
