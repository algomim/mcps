namespace Algomim.Aec.Mcp.Core.Operations.Modify;

using Algomim.Aec.Mcp.Core.Geometry;
using static Algomim.Aec.Mcp.Core.Operations.PlanValidation;

public static class ElementMovePlanner
{
    public static ElementMovePlan CreatePlan(
        IReadOnlyList<long> elementIds,
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        IReadOnlyList<double> z)
    {
        EnsureNotEmpty(nameof(elementIds), elementIds.Count);
        EnsureCompatibleLength(nameof(x), elementIds.Count, x.Count);
        EnsureCompatibleLength(nameof(y), elementIds.Count, y.Count);
        EnsureCompatibleLength(nameof(z), elementIds.Count, z.Count);

        var items = Enumerable.Range(0, elementIds.Count)
            .Select(index => new ElementMoveItem(elementIds[index], new AecPoint3(ValueAt(x, index), ValueAt(y, index), ValueAt(z, index))))
            .ToList();

        return new ElementMovePlan(items);
    }
}

public static class ElementCopyPlanner
{
    public static ElementCopyPlan CreatePlan(
        IReadOnlyList<long> elementIds,
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        IReadOnlyList<double> z)
    {
        EnsureNotEmpty(nameof(elementIds), elementIds.Count);
        EnsureCompatibleLength(nameof(x), elementIds.Count, x.Count);
        EnsureCompatibleLength(nameof(y), elementIds.Count, y.Count);
        EnsureCompatibleLength(nameof(z), elementIds.Count, z.Count);

        var items = Enumerable.Range(0, elementIds.Count)
            .Select(index => new ElementCopyItem(elementIds[index], new AecPoint3(ValueAt(x, index), ValueAt(y, index), ValueAt(z, index))))
            .ToList();

        return new ElementCopyPlan(items);
    }
}

public static class ElementRotatePlanner
{
    public static ElementRotatePlan CreatePlan(
        IReadOnlyList<long> elementIds,
        IReadOnlyList<double> axisStartX,
        IReadOnlyList<double> axisStartY,
        IReadOnlyList<double> axisStartZ,
        IReadOnlyList<double> axisEndX,
        IReadOnlyList<double> axisEndY,
        IReadOnlyList<double> axisEndZ,
        IReadOnlyList<double> angles)
    {
        EnsureNotEmpty(nameof(elementIds), elementIds.Count);
        EnsureCompatibleLength(nameof(axisStartX), elementIds.Count, axisStartX.Count);
        EnsureCompatibleLength(nameof(axisStartY), elementIds.Count, axisStartY.Count);
        EnsureCompatibleLength(nameof(axisStartZ), elementIds.Count, axisStartZ.Count);
        EnsureCompatibleLength(nameof(axisEndX), elementIds.Count, axisEndX.Count);
        EnsureCompatibleLength(nameof(axisEndY), elementIds.Count, axisEndY.Count);
        EnsureCompatibleLength(nameof(axisEndZ), elementIds.Count, axisEndZ.Count);
        EnsureCompatibleLength(nameof(angles), elementIds.Count, angles.Count);

        var items = Enumerable.Range(0, elementIds.Count)
            .Select(index => new ElementRotateItem(
                elementIds[index],
                new AecPoint3(ValueAt(axisStartX, index), ValueAt(axisStartY, index), ValueAt(axisStartZ, index)),
                new AecPoint3(ValueAt(axisEndX, index), ValueAt(axisEndY, index), ValueAt(axisEndZ, index)),
                ValueAt(angles, index)))
            .ToList();

        return new ElementRotatePlan(items);
    }
}

public static class ElementDeletePlanner
{
    public static ElementDeletePlan CreatePlan(IReadOnlyList<long> elementIds)
    {
        EnsureNotEmpty(nameof(elementIds), elementIds.Count);
        return new ElementDeletePlan(elementIds);
    }
}

public static class ViewCopyFiltersPlanner
{
    public static ViewCopyFiltersPlan CreatePlan(long sourceViewId, IReadOnlyList<long> filterIds, IReadOnlyList<long> targetViewIds)
    {
        EnsureNotEmpty(nameof(filterIds), filterIds.Count);
        EnsureNotEmpty(nameof(targetViewIds), targetViewIds.Count);
        return new ViewCopyFiltersPlan(sourceViewId, filterIds, targetViewIds);
    }
}

public static class SheetSetRevisionsPlanner
{
    public static SheetSetRevisionsPlan CreatePlan(IReadOnlyList<long> sheetIds, IReadOnlyList<long> revisionIds, bool assign)
    {
        EnsureNotEmpty(nameof(sheetIds), sheetIds.Count);
        EnsureNotEmpty(nameof(revisionIds), revisionIds.Count);
        return new SheetSetRevisionsPlan(sheetIds, revisionIds, assign);
    }
}
