using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Create.ViewPlanCreate;

internal sealed record ViewPlanCreateArgs(
    IReadOnlyList<string> Names,
    IReadOnlyList<long> LevelIds,
    IReadOnlyList<bool> CeilingPlans)
{
    public static ViewPlanCreateArgs From(ArgumentReader arguments)
        => new(
            arguments.RequireStringArray("names", 500),
            arguments.RequireLongArray("levelIds", 500),
            arguments.RequireBoolArray("ceilingPlans", 500));
}
