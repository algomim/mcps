namespace Algomim.Aec.Mcp.Core.Operations.Modify;

using Algomim.Aec.Mcp.Core.Commands;
using Algomim.Aec.Mcp.Core.Geometry;

public sealed record ElementMovePlan(IReadOnlyList<ElementMoveItem> Items)
    : ToolPlan("element_move", ToolExecutionMode.Write);

public sealed record ElementMoveItem(long ElementId, AecPoint3 Offset);

public sealed record ElementCopyPlan(IReadOnlyList<ElementCopyItem> Items)
    : ToolPlan("element_copy", ToolExecutionMode.Write);

public sealed record ElementCopyItem(long ElementId, AecPoint3 Offset);

public sealed record ElementRotatePlan(IReadOnlyList<ElementRotateItem> Items)
    : ToolPlan("element_rotate", ToolExecutionMode.Write);

public sealed record ElementRotateItem(long ElementId, AecPoint3 AxisStart, AecPoint3 AxisEnd, double Angle);

public sealed record ElementDeletePlan(IReadOnlyList<long> ElementIds)
    : ToolPlan("element_delete", ToolExecutionMode.Write);

public sealed record ViewCopyFiltersPlan(long SourceViewId, IReadOnlyList<long> FilterIds, IReadOnlyList<long> TargetViewIds)
    : ToolPlan("view_copy_filters", ToolExecutionMode.Write);

public sealed record SheetSetRevisionsPlan(IReadOnlyList<long> SheetIds, IReadOnlyList<long> RevisionIds, bool Assign)
    : ToolPlan("sheet_set_revisions", ToolExecutionMode.Write);
