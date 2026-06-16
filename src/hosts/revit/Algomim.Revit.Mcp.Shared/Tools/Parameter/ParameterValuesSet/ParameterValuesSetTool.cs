using System.Text.Json;
using Algomim.Aec.Mcp.Core.Operations.Parameter;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Composition;

namespace Algomim.Revit.Mcp.Tools.Parameter.ParameterValuesSet;

internal sealed class ParameterValuesSetTool : RevitPlannedWriteToolBase<ParameterValuesSetPlan>
{
    private readonly RevitParameterValuesSetExecutor _executor;

    private ParameterValuesSetTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore, RevitParameterValuesSetExecutor executor)
        : base(dispatcher, documentContextStore) => _executor = executor;

    public override string Name => "parameter_set_values";
    public override string Description => "Set one parameter value for many element/type ids.";
    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new
        {
            elementIds = ArrayOf("integer"),
            parameterId = new { type = "integer" },
            values = ArrayOf("string"),
        },
        required = new[] { "elementIds", "parameterId", "values" },
    });
    public override ToolMetadata Metadata { get; } = new("parameter_set_values", ToolCategory.Parameter, ToolMode.Write, ToolRisk.High, "Set one parameter value for many element/type ids.");

    public static IMcpTool Create(RevitToolServices services)
        => new ParameterValuesSetTool(services.Dispatcher, services.DocumentContextStore, new RevitParameterValuesSetExecutor());

    protected override ParameterValuesSetPlan CreatePlan(ArgumentReader arguments)
    {
        var args = ParameterValuesSetArgs.From(arguments);
        return ParameterValuesSetPlanner.CreatePlan(args.ElementIds, args.ParameterId, args.Values);
    }

    protected override McpToolResult ExecutePlan(RevitToolContext context, ParameterValuesSetPlan plan)
        => _executor.Execute(context, plan);

    private static object ArrayOf(string type)
        => new { type = "array", items = new { type } };
}
