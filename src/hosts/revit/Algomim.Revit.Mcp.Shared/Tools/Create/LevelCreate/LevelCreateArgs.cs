using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Create.LevelCreate;

internal sealed record LevelCreateArgs(IReadOnlyList<string> Names, IReadOnlyList<double> Elevations)
{
    public static LevelCreateArgs From(ArgumentReader arguments)
        => new(arguments.RequireStringArray("names", 500), arguments.RequireDoubleArray("elevations", 500));
}
