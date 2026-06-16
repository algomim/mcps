using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Modify.ElementDelete;

internal sealed record ElementDeleteArgs(IReadOnlyList<long> ElementIds)
{
    public static ElementDeleteArgs From(ArgumentReader arguments)
        => new(arguments.RequireLongArray("elementIds", 5000));
}
