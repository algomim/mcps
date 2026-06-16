using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Parameter.PropertyValuesSet;

internal sealed record PropertyValuesSetArgs(
    string PropertyName,
    IReadOnlyList<long> ElementIds,
    IReadOnlyList<string> Values)
{
    public static PropertyValuesSetArgs From(ArgumentReader arguments)
        => new(
            arguments.RequireString("propertyName"),
            arguments.RequireLongArray("elementIds", 500),
            arguments.RequireStringArray("values", 500));
}
