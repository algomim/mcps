using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Modify.ElementRotate;

internal sealed record ElementRotateArgs(
    IReadOnlyList<long> ElementIds,
    IReadOnlyList<double> AxisStartX,
    IReadOnlyList<double> AxisStartY,
    IReadOnlyList<double> AxisStartZ,
    IReadOnlyList<double> AxisEndX,
    IReadOnlyList<double> AxisEndY,
    IReadOnlyList<double> AxisEndZ,
    IReadOnlyList<double> Angles)
{
    public static ElementRotateArgs From(ArgumentReader arguments)
        => new(
            arguments.RequireLongArray("elementIds", 100),
            arguments.RequireDoubleArray("axisStartX", 100),
            arguments.RequireDoubleArray("axisStartY", 100),
            arguments.RequireDoubleArray("axisStartZ", 100),
            arguments.RequireDoubleArray("axisEndX", 100),
            arguments.RequireDoubleArray("axisEndY", 100),
            arguments.RequireDoubleArray("axisEndZ", 100),
            arguments.RequireDoubleArray("angles", 100));
}
