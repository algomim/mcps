using Algomim.Aec.Mcp.Core.Operations.Parameter;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Parameter.ParameterValuesSet;

internal sealed class RevitParameterValuesSetExecutor
{
    public McpToolResult Execute(RevitToolContext context, ParameterValuesSetPlan plan)
    {
        var results = new List<object>();
        foreach (var item in plan.Items)
        {
            var element = context.Document.GetElement(RevitIds.Id(item.ElementId));
            var parameter = element is null ? null : RevitParameterAccess.FindParameterById(element, plan.ParameterId);
            string? setError = null;
            var ok = parameter is not null && RevitParameterAccess.TrySetParameter(parameter, item.RawValue, out setError);
            results.Add(new { elementId = item.ElementId, ok, error = parameter is null ? "Parameter not found." : setError });
        }

        return ToolResults.Success(new { count = results.Count, results }, $"{results.Count} parameter set attempt(s).");
    }
}
