using Algomim.Aec.Mcp.Core.Operations.Parameter;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Parameter.PropertyValuesSet;

internal sealed class RevitPropertyValuesSetExecutor
{
    public McpToolResult Execute(RevitToolContext context, PropertyValuesSetPlan plan)
    {
        var results = new List<object>();
        foreach (var item in plan.Items)
        {
            var element = context.Document.GetElement(RevitIds.Id(item.ElementId));
            string? setError = null;
            var ok = element is not null && RevitParameterAccess.TrySetPublicProperty(element, plan.PropertyName, item.RawValue, out setError);
            results.Add(new { elementId = item.ElementId, ok, error = element is null ? "Element not found." : setError });
        }

        return ToolResults.Success(new { count = results.Count, results }, $"{results.Count} property set attempt(s).");
    }
}
