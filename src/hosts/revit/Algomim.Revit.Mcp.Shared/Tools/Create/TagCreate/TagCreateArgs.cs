using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Create.TagCreate;

internal sealed record TagCreateArgs(
    IReadOnlyList<long> ElementIds,
    long ViewId,
    double OffsetX,
    double OffsetY,
    bool AddLeader,
    bool AddElbowHorizontalLeader)
{
    public static TagCreateArgs From(ArgumentReader arguments)
        => new(
            arguments.RequireLongArray("elementIds", 500),
            arguments.RequireLong("viewId"),
            arguments.RequireDouble("offsetX"),
            arguments.RequireDouble("offsetY"),
            arguments.RequireBool("addLeader"),
            arguments.RequireBool("addElbowHorizontalLeader"));
}
