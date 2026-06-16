using Autodesk.Revit.DB;

namespace Algomim.Revit.Mcp.Tools.Common;

internal static class RevitIds
{
    public static long Value(ElementId id) => id.Value;

    public static ElementId Id(long value) => new(value);

    public static List<ElementId> ToElementIds(IEnumerable<long> ids)
        => ids.Select(Id).ToList();

    public static object Shape(ElementId id) => new { id = Value(id) };
}
