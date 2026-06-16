using System.Text.Json;
using Algomim.Aec.Mcp.Core.Operations.Graphics;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Composition;

namespace Algomim.Revit.Mcp.Tools.Graphics.ElementOverridesSet;

internal sealed class ElementOverridesSetTool : RevitPlannedWriteToolBase<ElementOverridesSetPlan>
{
    private readonly RevitElementOverridesSetExecutor _executor;

    private ElementOverridesSetTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore, RevitElementOverridesSetExecutor executor)
        : base(dispatcher, documentContextStore) => _executor = executor;

    public override string Name => "graphics_set_element_overrides";
    public override string Description => "Set or clear element graphic overrides in a view.";
    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new
        {
            viewId = new { type = "integer" },
            elementIds = ArrayOf("integer"),
            red = new { type = "integer" },
            green = new { type = "integer" },
            blue = new { type = "integer" },
            clear = new { type = "boolean" },
        },
        required = new[] { "viewId", "elementIds" },
    });
    public override ToolMetadata Metadata { get; } = new("graphics_set_element_overrides", ToolCategory.Graphics, ToolMode.Write, ToolRisk.High, "Set or clear element graphic overrides in a view.");

    public static IMcpTool Create(RevitToolServices services)
        => new ElementOverridesSetTool(services.Dispatcher, services.DocumentContextStore, new RevitElementOverridesSetExecutor());

    protected override ElementOverridesSetPlan CreatePlan(ArgumentReader arguments)
    {
        var args = ElementOverridesSetArgs.From(arguments);
        return ElementOverridesSetPlanner.CreatePlan(args.ViewId, args.ElementIds, args.Clear, args.Red, args.Green, args.Blue);
    }

    protected override McpToolResult ExecutePlan(RevitToolContext context, ElementOverridesSetPlan plan)
        => _executor.Execute(context, plan);

    private static object ArrayOf(string type)
        => new { type = "array", items = new { type } };
}
