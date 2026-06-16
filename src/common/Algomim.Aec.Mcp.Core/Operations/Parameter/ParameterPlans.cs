namespace Algomim.Aec.Mcp.Core.Operations.Parameter;

using Algomim.Aec.Mcp.Core.Commands;

public sealed record ParameterValuesSetPlan(long ParameterId, IReadOnlyList<ParameterValueSetItem> Items)
    : ToolPlan("parameter_set_values", ToolExecutionMode.Write);

public sealed record ParameterValueSetItem(long ElementId, string RawValue);

public sealed record PropertyValuesSetPlan(string PropertyName, IReadOnlyList<PropertyValueSetItem> Items)
    : ToolPlan("property_set_values", ToolExecutionMode.Write);

public sealed record PropertyValueSetItem(long ElementId, string RawValue);
