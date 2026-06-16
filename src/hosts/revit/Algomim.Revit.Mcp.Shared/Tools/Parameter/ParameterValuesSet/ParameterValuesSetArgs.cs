using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Parameter.ParameterValuesSet;

internal sealed record ParameterValuesSetArgs(
    IReadOnlyList<long> ElementIds,
    long ParameterId,
    IReadOnlyList<string> Values)
{
    public static ParameterValuesSetArgs From(ArgumentReader arguments)
        => new(
            arguments.RequireLongArray("elementIds", 1000),
            arguments.RequireLong("parameterId"),
            arguments.RequireStringArray("values", 1000));
}
