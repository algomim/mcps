using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Create.GridCreate;

internal sealed record GridCreateArgs(
    IReadOnlyList<string> Names,
    IReadOnlyList<double> StartX,
    IReadOnlyList<double> StartY,
    IReadOnlyList<double> StartZ,
    IReadOnlyList<double> EndX,
    IReadOnlyList<double> EndY,
    IReadOnlyList<double> EndZ)
{
    public static GridCreateArgs From(ArgumentReader arguments)
        => new(
            arguments.RequireStringArray("names", 500),
            arguments.RequireDoubleArray("startX", 500),
            arguments.RequireDoubleArray("startY", 500),
            arguments.RequireDoubleArray("startZ", 500),
            arguments.RequireDoubleArray("endX", 500),
            arguments.RequireDoubleArray("endY", 500),
            arguments.RequireDoubleArray("endZ", 500));
}
