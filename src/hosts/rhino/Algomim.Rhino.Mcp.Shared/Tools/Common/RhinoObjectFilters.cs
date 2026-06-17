using Rhino;
using Rhino.DocObjects;

namespace Algomim.Rhino.Mcp.Tools.Common;

internal static class RhinoObjectFilters
{
    public static ObjectEnumeratorSettings ActiveObjects(bool includeHidden, bool includeLocked)
        => new()
        {
            ActiveObjects = true,
            HiddenObjects = includeHidden,
            LockedObjects = includeLocked,
            DeletedObjects = false,
            IncludeLights = true,
            IncludeGrips = false,
        };

    public static bool TryApplyGeometryType(ObjectEnumeratorSettings settings, string? geometryType)
    {
        if (string.IsNullOrWhiteSpace(geometryType))
            return true;

        if (!TryParseObjectType(geometryType, out var objectType))
            return false;

        settings.ObjectTypeFilter = objectType;
        return true;
    }

    public static int FindLayerIndex(RhinoDoc document, string layerNameOrPath)
    {
        var index = document.Layers.FindByFullPath(layerNameOrPath, RhinoMath.UnsetIntIndex);
        if (index >= 0)
            return index;

        for (var i = 0; i < document.Layers.Count; i++)
        {
            var layer = document.Layers[i];
            if (layer is null)
                continue;

            if (string.Equals(layer.FullPath, layerNameOrPath, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(layer.Name, layerNameOrPath, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool TryParseObjectType(string value, out ObjectType objectType)
    {
        objectType = value.Trim().ToLowerInvariant() switch
        {
            "point" => ObjectType.Point,
            "pointset" => ObjectType.PointSet,
            "curve" => ObjectType.Curve,
            "surface" => ObjectType.Surface,
            "brep" => ObjectType.Brep,
            "mesh" => ObjectType.Mesh,
            "annotation" => ObjectType.Annotation,
            "light" => ObjectType.Light,
            "block" => ObjectType.InstanceReference,
            "instance" => ObjectType.InstanceReference,
            _ => ObjectType.None,
        };

        return objectType != ObjectType.None;
    }
}
