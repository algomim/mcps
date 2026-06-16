using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Graphics.ElementOverridesSet;

internal sealed record ElementOverridesSetArgs(
    long ViewId,
    IReadOnlyList<long> ElementIds,
    bool Clear,
    int Red,
    int Green,
    int Blue)
{
    public static ElementOverridesSetArgs From(ArgumentReader arguments)
        => new(
            arguments.RequireLong("viewId"),
            arguments.RequireLongArray("elementIds", 1000),
            arguments.OptionalBool("clear", false),
            arguments.OptionalInt("red", 255, 0, 255),
            arguments.OptionalInt("green", 0, 0, 255),
            arguments.OptionalInt("blue", 0, 0, 255));
}
