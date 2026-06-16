using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Modify.ElementCopy;

internal sealed record ElementCopyArgs(
    IReadOnlyList<long> ElementIds,
    IReadOnlyList<double> X,
    IReadOnlyList<double> Y,
    IReadOnlyList<double> Z)
{
    public static ElementCopyArgs From(ArgumentReader arguments)
        => new(
            arguments.RequireLongArray("elementIds", 100),
            arguments.RequireDoubleArray("x", 100),
            arguments.RequireDoubleArray("y", 100),
            arguments.RequireDoubleArray("z", 100));
}
