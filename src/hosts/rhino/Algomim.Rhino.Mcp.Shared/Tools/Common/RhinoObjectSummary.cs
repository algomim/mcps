using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace Algomim.Rhino.Mcp.Tools.Common;

internal static class RhinoObjectSummary
{
    public static object From(RhinoDoc document, RhinoObject obj)
    {
        var geometry = obj.Geometry;
        return new
        {
            id = obj.Id.ToString("D"),
            name = EmptyToNull(obj.Name),
            layer = LayerName(document, obj.Attributes.LayerIndex),
            layerIndex = obj.Attributes.LayerIndex,
            objectType = obj.ObjectType.ToString(),
            geometryType = geometry?.GetType().Name ?? "Unknown",
            boundingBox = BoundingBox(geometry),
        };
    }

    private static string? LayerName(RhinoDoc document, int layerIndex)
    {
        if (layerIndex < 0)
            return null;

        try
        {
            var layer = document.Layers[layerIndex];
            return EmptyToNull(layer?.FullPath) ?? EmptyToNull(layer?.Name);
        }
        catch
        {
            return null;
        }
    }

    private static object? BoundingBox(GeometryBase? geometry)
    {
        if (geometry is null)
            return null;

        var box = geometry.GetBoundingBox(true);
        if (!box.IsValid)
            return null;

        return new
        {
            min = Point(box.Min),
            max = Point(box.Max),
        };
    }

    private static object Point(Point3d point)
        => new { x = point.X, y = point.Y, z = point.Z };

    private static string? EmptyToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
