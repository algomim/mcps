namespace Algomim.Aec.Mcp.Core.Operations.Create;

using Algomim.Aec.Mcp.Core.Commands;
using Algomim.Aec.Mcp.Core.Geometry;

public sealed record GridCreatePlan(IReadOnlyList<GridLineCreatePlan> Lines)
    : ToolPlan("create_grids", ToolExecutionMode.Write);

public sealed record GridLineCreatePlan(string Name, AecPoint3 Start, AecPoint3 End);

public sealed record LevelCreatePlan(IReadOnlyList<LevelCreateItem> Levels)
    : ToolPlan("create_levels", ToolExecutionMode.Write);

public sealed record LevelCreateItem(string Name, double Elevation);

public sealed record ViewPlanCreatePlan(IReadOnlyList<ViewPlanCreateItem> Views)
    : ToolPlan("create_view_plans", ToolExecutionMode.Write);

public sealed record ViewPlanCreateItem(string Name, long LevelId, bool IsCeilingPlan);

public sealed record View3DCreatePlan(IReadOnlyList<string> Names)
    : ToolPlan("create_view_3ds", ToolExecutionMode.Write);

public sealed record ViewSectionCreatePlan(IReadOnlyList<ViewSectionCreateItem> Views)
    : ToolPlan("create_view_sections", ToolExecutionMode.Write);

public sealed record ViewSectionCreateItem(
    string Name,
    AecPoint3 Start,
    AecPoint3 End,
    double Depth,
    double Height,
    bool IsDetailView);

public sealed record SheetCreatePlan(IReadOnlyList<SheetCreateItem> Sheets)
    : ToolPlan("create_sheets", ToolExecutionMode.Write);

public sealed record SheetCreateItem(string Name, long? TitleblockTypeId);

public sealed record TagCreatePlan(
    IReadOnlyList<long> ElementIds,
    long ViewId,
    double OffsetX,
    double OffsetY,
    bool AddLeader,
    bool AddElbowHorizontalLeader)
    : ToolPlan("create_tags", ToolExecutionMode.Write);

public sealed record ScheduleCreatePlan(string Name, long CategoryId, IReadOnlyList<long> ParameterIds)
    : ToolPlan("create_schedule", ToolExecutionMode.Write);

public sealed record DraftingOrLegendViewCreatePlan(IReadOnlyList<DraftingOrLegendViewCreateItem> Views)
    : ToolPlan("create_drafting_or_legend_views", ToolExecutionMode.Write);

public sealed record DraftingOrLegendViewCreateItem(string Name, bool IsDraftingView);

public sealed record TextNotesCreatePlan(long ViewId, IReadOnlyList<TextNoteCreateItem> Notes)
    : ToolPlan("create_text_notes", ToolExecutionMode.Write);

public sealed record TextNoteCreateItem(string Text, AecPoint3 Position);

public sealed record RoomElevationCreatePlan(IReadOnlyList<RoomElevationCreateItem> Rooms)
    : ToolPlan("create_room_elevation_views", ToolExecutionMode.Write);

public sealed record RoomElevationCreateItem(
    long RoomId,
    long ViewPlanId,
    int Scale,
    bool IncludeNorth,
    bool IncludeWest,
    bool IncludeSouth,
    bool IncludeEast);
