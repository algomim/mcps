namespace Algomim.Aec.Mcp.Core.Operations.Parameter;

using static Algomim.Aec.Mcp.Core.Operations.PlanValidation;

public static class ParameterValuesSetPlanner
{
    public static ParameterValuesSetPlan CreatePlan(
        IReadOnlyList<long> elementIds,
        long parameterId,
        IReadOnlyList<string> values)
    {
        EnsureNotEmpty(nameof(elementIds), elementIds.Count);
        EnsureCompatibleLength(nameof(values), elementIds.Count, values.Count);

        var items = Enumerable.Range(0, elementIds.Count)
            .Select(index => new ParameterValueSetItem(elementIds[index], ValueAt(values, index)))
            .ToList();

        return new ParameterValuesSetPlan(parameterId, items);
    }
}

public static class PropertyValuesSetPlanner
{
    public static PropertyValuesSetPlan CreatePlan(
        string propertyName,
        IReadOnlyList<long> elementIds,
        IReadOnlyList<string> values)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            throw new Validation.ToolPlanValidationException(nameof(propertyName), "required string is missing or empty");

        EnsureNotEmpty(nameof(elementIds), elementIds.Count);
        EnsureCompatibleLength(nameof(values), elementIds.Count, values.Count);

        var items = Enumerable.Range(0, elementIds.Count)
            .Select(index => new PropertyValueSetItem(elementIds[index], ValueAt(values, index)))
            .ToList();

        return new PropertyValuesSetPlan(propertyName, items);
    }
}
