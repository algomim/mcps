using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Create.RoomElevationCreate;

internal sealed record RoomElevationCreateArgs(
    IReadOnlyList<long> RoomIds,
    IReadOnlyList<long> ViewPlanIds,
    IReadOnlyList<long> Scales,
    IReadOnlyList<bool> IncludeNorth,
    IReadOnlyList<bool> IncludeWest,
    IReadOnlyList<bool> IncludeSouth,
    IReadOnlyList<bool> IncludeEast)
{
    public static RoomElevationCreateArgs From(ArgumentReader arguments)
        => new(
            arguments.RequireLongArray("roomIds", 100),
            arguments.RequireLongArray("viewPlanIds", 100),
            arguments.RequireLongArray("scales", 100),
            arguments.RequireBoolArray("includeNorth", 100),
            arguments.RequireBoolArray("includeWest", 100),
            arguments.RequireBoolArray("includeSouth", 100),
            arguments.RequireBoolArray("includeEast", 100));
}
