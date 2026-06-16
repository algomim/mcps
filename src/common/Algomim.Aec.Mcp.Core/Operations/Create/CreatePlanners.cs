namespace Algomim.Aec.Mcp.Core.Operations.Create;

using Algomim.Aec.Mcp.Core.Geometry;
using static Algomim.Aec.Mcp.Core.Operations.PlanValidation;

public static class GridCreatePlanner
{
    public static GridCreatePlan CreatePlan(
        IReadOnlyList<string> names,
        IReadOnlyList<double> startX,
        IReadOnlyList<double> startY,
        IReadOnlyList<double> startZ,
        IReadOnlyList<double> endX,
        IReadOnlyList<double> endY,
        IReadOnlyList<double> endZ)
    {
        EnsureNotEmpty(nameof(names), names.Count);
        EnsureCompatibleLength(nameof(startX), names.Count, startX.Count);
        EnsureCompatibleLength(nameof(startY), names.Count, startY.Count);
        EnsureCompatibleLength(nameof(startZ), names.Count, startZ.Count);
        EnsureCompatibleLength(nameof(endX), names.Count, endX.Count);
        EnsureCompatibleLength(nameof(endY), names.Count, endY.Count);
        EnsureCompatibleLength(nameof(endZ), names.Count, endZ.Count);

        var lines = Enumerable.Range(0, names.Count)
            .Select(index => new GridLineCreatePlan(
                names[index],
                new AecPoint3(ValueAt(startX, index), ValueAt(startY, index), ValueAt(startZ, index)),
                new AecPoint3(ValueAt(endX, index), ValueAt(endY, index), ValueAt(endZ, index))))
            .ToList();

        return new GridCreatePlan(lines);
    }
}

public static class LevelCreatePlanner
{
    public static LevelCreatePlan CreatePlan(IReadOnlyList<string> names, IReadOnlyList<double> elevations)
    {
        EnsureNotEmpty(nameof(names), names.Count);
        EnsureCompatibleLength(nameof(elevations), names.Count, elevations.Count);

        var levels = Enumerable.Range(0, names.Count)
            .Select(index => new LevelCreateItem(names[index], ValueAt(elevations, index)))
            .ToList();

        return new LevelCreatePlan(levels);
    }
}

public static class ViewPlanCreatePlanner
{
    public static ViewPlanCreatePlan CreatePlan(
        IReadOnlyList<string> names,
        IReadOnlyList<long> levelIds,
        IReadOnlyList<bool> ceilingPlans)
    {
        EnsureNotEmpty(nameof(names), names.Count);
        EnsureCompatibleLength(nameof(levelIds), names.Count, levelIds.Count);
        EnsureCompatibleLength(nameof(ceilingPlans), names.Count, ceilingPlans.Count);

        var views = Enumerable.Range(0, names.Count)
            .Select(index => new ViewPlanCreateItem(names[index], ValueAt(levelIds, index), ValueAt(ceilingPlans, index)))
            .ToList();

        return new ViewPlanCreatePlan(views);
    }
}

public static class SheetCreatePlanner
{
    public static SheetCreatePlan CreatePlan(IReadOnlyList<string> names, IReadOnlyList<long> titleblockTypeIds)
    {
        EnsureNotEmpty(nameof(names), names.Count);
        if (titleblockTypeIds.Count > 0)
            EnsureCompatibleLength(nameof(titleblockTypeIds), names.Count, titleblockTypeIds.Count);

        var sheets = Enumerable.Range(0, names.Count)
            .Select(index => new SheetCreateItem(
                names[index],
                titleblockTypeIds.Count == 0 ? null : ValueAt(titleblockTypeIds, index)))
            .ToList();

        return new SheetCreatePlan(sheets);
    }
}

public static class View3DCreatePlanner
{
    public static View3DCreatePlan CreatePlan(IReadOnlyList<string> names)
    {
        EnsureNotEmpty(nameof(names), names.Count);
        return new View3DCreatePlan(names);
    }
}

public static class ViewSectionCreatePlanner
{
    public static ViewSectionCreatePlan CreatePlan(
        IReadOnlyList<string> names,
        IReadOnlyList<double> startX,
        IReadOnlyList<double> startY,
        IReadOnlyList<double> startZ,
        IReadOnlyList<double> endX,
        IReadOnlyList<double> endY,
        IReadOnlyList<double> endZ,
        IReadOnlyList<double> depths,
        IReadOnlyList<double> heights,
        IReadOnlyList<bool> detailViews)
    {
        EnsureNotEmpty(nameof(names), names.Count);
        EnsureCompatibleLength(nameof(startX), names.Count, startX.Count);
        EnsureCompatibleLength(nameof(startY), names.Count, startY.Count);
        EnsureCompatibleLength(nameof(startZ), names.Count, startZ.Count);
        EnsureCompatibleLength(nameof(endX), names.Count, endX.Count);
        EnsureCompatibleLength(nameof(endY), names.Count, endY.Count);
        EnsureCompatibleLength(nameof(endZ), names.Count, endZ.Count);
        EnsureCompatibleLength(nameof(depths), names.Count, depths.Count);
        EnsureCompatibleLength(nameof(heights), names.Count, heights.Count);
        EnsureCompatibleLength(nameof(detailViews), names.Count, detailViews.Count);

        var views = Enumerable.Range(0, names.Count)
            .Select(index => new ViewSectionCreateItem(
                names[index],
                new AecPoint3(ValueAt(startX, index), ValueAt(startY, index), ValueAt(startZ, index)),
                new AecPoint3(ValueAt(endX, index), ValueAt(endY, index), ValueAt(endZ, index)),
                ValueAt(depths, index),
                ValueAt(heights, index),
                ValueAt(detailViews, index)))
            .ToList();

        return new ViewSectionCreatePlan(views);
    }
}

public static class ScheduleCreatePlanner
{
    public static ScheduleCreatePlan CreatePlan(string name, long categoryId, IReadOnlyList<long> parameterIds)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new Validation.ToolPlanValidationException(nameof(name), "required string is missing or empty");

        return new ScheduleCreatePlan(name, categoryId, parameterIds);
    }
}

public static class DraftingOrLegendViewCreatePlanner
{
    public static DraftingOrLegendViewCreatePlan CreatePlan(IReadOnlyList<string> names, IReadOnlyList<bool> draftingViews)
    {
        EnsureNotEmpty(nameof(names), names.Count);
        EnsureCompatibleLength(nameof(draftingViews), names.Count, draftingViews.Count);

        var views = Enumerable.Range(0, names.Count)
            .Select(index => new DraftingOrLegendViewCreateItem(names[index], ValueAt(draftingViews, index)))
            .ToList();

        return new DraftingOrLegendViewCreatePlan(views);
    }
}

public static class TextNotesCreatePlanner
{
    public static TextNotesCreatePlan CreatePlan(
        long viewId,
        IReadOnlyList<string> texts,
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        IReadOnlyList<double> z)
    {
        EnsureNotEmpty(nameof(texts), texts.Count);
        EnsureCompatibleLength(nameof(x), texts.Count, x.Count);
        EnsureCompatibleLength(nameof(y), texts.Count, y.Count);
        EnsureCompatibleLength(nameof(z), texts.Count, z.Count);

        var notes = Enumerable.Range(0, texts.Count)
            .Select(index => new TextNoteCreateItem(texts[index], new AecPoint3(ValueAt(x, index), ValueAt(y, index), ValueAt(z, index))))
            .ToList();

        return new TextNotesCreatePlan(viewId, notes);
    }
}

public static class RoomElevationCreatePlanner
{
    public static RoomElevationCreatePlan CreatePlan(
        IReadOnlyList<long> roomIds,
        IReadOnlyList<long> viewPlanIds,
        IReadOnlyList<long> scales,
        IReadOnlyList<bool> includeNorth,
        IReadOnlyList<bool> includeWest,
        IReadOnlyList<bool> includeSouth,
        IReadOnlyList<bool> includeEast)
    {
        EnsureNotEmpty(nameof(roomIds), roomIds.Count);
        EnsureCompatibleLength(nameof(viewPlanIds), roomIds.Count, viewPlanIds.Count);
        EnsureCompatibleLength(nameof(scales), roomIds.Count, scales.Count);
        EnsureCompatibleLength(nameof(includeNorth), roomIds.Count, includeNorth.Count);
        EnsureCompatibleLength(nameof(includeWest), roomIds.Count, includeWest.Count);
        EnsureCompatibleLength(nameof(includeSouth), roomIds.Count, includeSouth.Count);
        EnsureCompatibleLength(nameof(includeEast), roomIds.Count, includeEast.Count);

        var rooms = Enumerable.Range(0, roomIds.Count)
            .Select(index => new RoomElevationCreateItem(
                roomIds[index],
                ValueAt(viewPlanIds, index),
                checked((int)ValueAt(scales, index)),
                ValueAt(includeNorth, index),
                ValueAt(includeWest, index),
                ValueAt(includeSouth, index),
                ValueAt(includeEast, index)))
            .ToList();

        return new RoomElevationCreatePlan(rooms);
    }
}

public static class TagCreatePlanner
{
    public static TagCreatePlan CreatePlan(
        IReadOnlyList<long> elementIds,
        long viewId,
        double offsetX,
        double offsetY,
        bool addLeader,
        bool addElbowHorizontalLeader)
    {
        EnsureNotEmpty(nameof(elementIds), elementIds.Count);
        return new TagCreatePlan(elementIds, viewId, offsetX, offsetY, addLeader, addElbowHorizontalLeader);
    }
}
