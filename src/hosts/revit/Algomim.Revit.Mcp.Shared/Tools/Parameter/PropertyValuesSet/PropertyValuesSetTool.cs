using System.Text.Json;
using Algomim.Aec.Mcp.Core.Operations.Parameter;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Composition;

namespace Algomim.Revit.Mcp.Tools.Parameter.PropertyValuesSet;

internal sealed class PropertyValuesSetTool : RevitPlannedWriteToolBase<PropertyValuesSetPlan>
{
    private readonly RevitPropertyValuesSetExecutor _executor;

    private PropertyValuesSetTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore, RevitPropertyValuesSetExecutor executor)
        : base(dispatcher, documentContextStore) => _executor = executor;

    public override string Name => "property_set_values";
    public override string Description => "Set one writable scalar public property for many elements.";
    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new
        {
            propertyName = new { type = "string" },
            elementIds = ArrayOf("integer"),
            values = ArrayOf("string"),
        },
        required = new[] { "propertyName", "elementIds", "values" },
    });
    public override ToolMetadata Metadata { get; } = new("property_set_values", ToolCategory.Parameter, ToolMode.Write, ToolRisk.High, "Set one writable scalar public property for many elements.");

    public static IMcpTool Create(RevitToolServices services)
        => new PropertyValuesSetTool(services.Dispatcher, services.DocumentContextStore, new RevitPropertyValuesSetExecutor());

    protected override PropertyValuesSetPlan CreatePlan(ArgumentReader arguments)
    {
        var args = PropertyValuesSetArgs.From(arguments);
        return PropertyValuesSetPlanner.CreatePlan(args.PropertyName, args.ElementIds, args.Values);
    }

    protected override McpToolResult ExecutePlan(RevitToolContext context, PropertyValuesSetPlan plan)
        => _executor.Execute(context, plan);

    private static object ArrayOf(string type)
        => new { type = "array", items = new { type } };
}
