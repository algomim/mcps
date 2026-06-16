namespace Algomim.Aec.Mcp.Core.Operations.Graphics;

using static Algomim.Aec.Mcp.Core.Operations.PlanValidation;

public static class ElementOverridesSetPlanner
{
    public static ElementOverridesSetPlan CreatePlan(
        long viewId,
        IReadOnlyList<long> elementIds,
        bool clear,
        int red,
        int green,
        int blue)
    {
        EnsureNotEmpty(nameof(elementIds), elementIds.Count);
        EnsureByteRange(nameof(red), red);
        EnsureByteRange(nameof(green), green);
        EnsureByteRange(nameof(blue), blue);
        return new ElementOverridesSetPlan(viewId, elementIds, clear, red, green, blue);
    }
}
