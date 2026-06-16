using Algomim.Aec.Mcp.Core.Operations.Create;
using Algomim.Aec.Mcp.Core.Operations.Export;
using Algomim.Aec.Mcp.Core.Operations.Graphics;
using Algomim.Aec.Mcp.Core.Operations.Modify;
using Algomim.Aec.Mcp.Core.Operations.Parameter;
using Algomim.Aec.Mcp.Core.Validation;
using Xunit;

namespace Algomim.Revit.Mcp.Tests;

public class PurePlannerTests
{
    [Fact]
    public void Level_create_planner_broadcasts_single_elevation()
    {
        var plan = LevelCreatePlanner.CreatePlan(["Level 1", "Level 2"], [10]);

        Assert.Equal(2, plan.Levels.Count);
        Assert.Equal(10, plan.Levels[0].Elevation);
        Assert.Equal(10, plan.Levels[1].Elevation);
    }

    [Fact]
    public void Element_move_planner_rejects_mismatched_vectors()
    {
        var ex = Assert.Throws<ToolPlanValidationException>(() =>
            ElementMovePlanner.CreatePlan([1, 2, 3], [1, 2], [0], [0]));

        Assert.Equal("x", ex.Field);
    }

    [Fact]
    public void Element_rotate_planner_expands_axis_and_angles()
    {
        var plan = ElementRotatePlanner.CreatePlan(
            [101, 102],
            [0], [0], [0],
            [0], [0], [1],
            [1.57]);

        Assert.Equal(2, plan.Items.Count);
        Assert.Equal(102, plan.Items[1].ElementId);
        Assert.Equal(1, plan.Items[1].AxisEnd.Z);
        Assert.Equal(1.57, plan.Items[1].Angle);
    }

    [Fact]
    public void Parameter_set_planner_broadcasts_single_value()
    {
        var plan = ParameterValuesSetPlanner.CreatePlan([11, 12], 99, ["A"]);

        Assert.Equal(99, plan.ParameterId);
        Assert.Equal("A", plan.Items[0].RawValue);
        Assert.Equal("A", plan.Items[1].RawValue);
    }

    [Fact]
    public void Export_planner_requires_view_ids_and_matching_file_names()
    {
        Assert.Throws<ToolPlanValidationException>(() => PdfExportPlanner.CreatePlan(["a"], [], "C:/tmp", false));
        Assert.Throws<ToolPlanValidationException>(() => CadExportPlanner.CreatePlan(["a", "b"], [1, 2, 3], "C:/tmp", true));
    }

    [Fact]
    public void Graphics_override_planner_rejects_invalid_colors()
    {
        var ex = Assert.Throws<ToolPlanValidationException>(() =>
            ElementOverridesSetPlanner.CreatePlan(7, [1], false, 300, 0, 0));

        Assert.Equal("red", ex.Field);
    }

    [Fact]
    public void Sheet_create_planner_keeps_optional_titleblock_null()
    {
        var plan = SheetCreatePlanner.CreatePlan(["A101"], []);

        Assert.Null(plan.Sheets[0].TitleblockTypeId);
    }

    [Fact]
    public void Tag_create_planner_rejects_empty_element_ids()
    {
        var ex = Assert.Throws<ToolPlanValidationException>(() =>
            TagCreatePlanner.CreatePlan([], 10, 0, 0, true, false));

        Assert.Equal("elementIds", ex.Field);
    }

    [Fact]
    public void View_section_planner_broadcasts_section_geometry()
    {
        var plan = ViewSectionCreatePlanner.CreatePlan(
            ["A", "B"],
            [0], [0], [0],
            [10], [0], [0],
            [5], [8],
            [false]);

        Assert.Equal(2, plan.Views.Count);
        Assert.Equal(10, plan.Views[1].End.X);
        Assert.False(plan.Views[1].IsDetailView);
    }

    [Fact]
    public void Text_notes_planner_broadcasts_position_values()
    {
        var plan = TextNotesCreatePlanner.CreatePlan(12, ["one", "two"], [1], [2], [3]);

        Assert.Equal(2, plan.Notes.Count);
        Assert.Equal(3, plan.Notes[1].Position.Z);
    }

    [Fact]
    public void Room_elevation_planner_maps_include_flags_in_order()
    {
        var plan = RoomElevationCreatePlanner.CreatePlan([10], [20], [50], [true], [false], [true], [false]);

        Assert.Equal(50, plan.Rooms[0].Scale);
        Assert.True(plan.Rooms[0].IncludeNorth);
        Assert.True(plan.Rooms[0].IncludeSouth);
    }

    [Fact]
    public void Property_set_planner_broadcasts_single_value()
    {
        var plan = PropertyValuesSetPlanner.CreatePlan("Name", [1, 2], ["Door"]);

        Assert.Equal("Name", plan.PropertyName);
        Assert.Equal("Door", plan.Items[1].RawValue);
    }

    [Fact]
    public void View_copy_and_sheet_revision_planners_reject_empty_targets()
    {
        Assert.Throws<ToolPlanValidationException>(() => ViewCopyFiltersPlanner.CreatePlan(1, [], [2]));
        Assert.Throws<ToolPlanValidationException>(() => SheetSetRevisionsPlanner.CreatePlan([1], [], true));
    }
}
