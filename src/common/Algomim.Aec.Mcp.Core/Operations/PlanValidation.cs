namespace Algomim.Aec.Mcp.Core.Operations;

using Algomim.Aec.Mcp.Core.Validation;

/// <summary>Pure input guards shared by host-neutral operation planners.</summary>
public static class PlanValidation
{
    public static void EnsureNotEmpty(string field, int count)
    {
        if (count > 0)
            return;

        throw new ToolPlanValidationException(field, "must contain at least one item");
    }

    public static void EnsureCompatibleLength(string field, int itemCount, int valueCount)
    {
        if (valueCount is 1 || valueCount == itemCount)
            return;

        throw new ToolPlanValidationException(field, "must contain either one value or the same number of values as the primary input");
    }

    public static void EnsureByteRange(string field, int value)
    {
        if (value is >= 0 and <= 255)
            return;

        throw new ToolPlanValidationException(field, "must be between 0 and 255");
    }

    public static T ValueAt<T>(IReadOnlyList<T> values, int index)
    {
        if (values.Count == 0)
            throw new ToolPlanValidationException("values", "list cannot be empty");

        return values.Count == 1 ? values[0] : values[index];
    }
}
