namespace Algomim.Aec.Mcp.Core.Operations.Graphics;

using Algomim.Aec.Mcp.Core.Commands;

public sealed record ElementOverridesSetPlan(
    long ViewId,
    IReadOnlyList<long> ElementIds,
    bool Clear,
    int Red,
    int Green,
    int Blue)
    : ToolPlan("graphics_set_element_overrides", ToolExecutionMode.Write);
