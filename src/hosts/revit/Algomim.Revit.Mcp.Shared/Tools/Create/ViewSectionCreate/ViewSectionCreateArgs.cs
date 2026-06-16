using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Create.ViewSectionCreate;

internal sealed record ViewSectionCreateArgs(
    IReadOnlyList<string> Names,
    IReadOnlyList<double> StartX,
    IReadOnlyList<double> StartY,
    IReadOnlyList<double> StartZ,
    IReadOnlyList<double> EndX,
    IReadOnlyList<double> EndY,
    IReadOnlyList<double> EndZ,
    IReadOnlyList<double> Depths,
    IReadOnlyList<double> Heights,
    IReadOnlyList<bool> DetailViews)
{
    public static ViewSectionCreateArgs From(ArgumentReader arguments)
        => new(
            arguments.RequireStringArray("names", 100),
            arguments.RequireDoubleArray("startX", 100),
            arguments.RequireDoubleArray("startY", 100),
            arguments.RequireDoubleArray("startZ", 100),
            arguments.RequireDoubleArray("endX", 100),
            arguments.RequireDoubleArray("endY", 100),
            arguments.RequireDoubleArray("endZ", 100),
            arguments.RequireDoubleArray("depths", 100),
            arguments.RequireDoubleArray("heights", 100),
            arguments.RequireBoolArray("detailViews", 100));
}
