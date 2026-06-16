using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Modify.ViewCopyFilters;

internal sealed record ViewCopyFiltersArgs(long SourceViewId, IReadOnlyList<long> FilterIds, IReadOnlyList<long> TargetViewIds)
{
    public static ViewCopyFiltersArgs From(ArgumentReader arguments)
        => new(
            arguments.RequireLong("sourceViewId"),
            arguments.RequireLongArray("filterIds", 100),
            arguments.RequireLongArray("targetViewIds", 100));
}
