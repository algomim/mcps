using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace Algomim.Rhino.Mcp.Tools.Common;

internal static class RhinoIds
{
    public static RhinoObject RequireObject(RhinoDoc document, Guid id)
        => document.Objects.FindId(id) ?? throw new ArgumentException($"Rhino object was not found: {id:D}");

    public static Curve RequireCurve(RhinoDoc document, Guid id)
    {
        var obj = RequireObject(document, id);
        return obj.Geometry as Curve ?? throw new ArgumentException($"Rhino object is not a curve: {id:D}");
    }

    public static Brep RequireBrep(RhinoDoc document, Guid id)
    {
        var obj = RequireObject(document, id);
        return ToBrep(obj) ?? throw new ArgumentException($"Rhino object cannot be converted to a Brep: {id:D}");
    }

    public static Brep? ToBrep(RhinoObject obj)
        => obj.Geometry switch
        {
            Brep brep => brep,
            Extrusion extrusion => extrusion.ToBrep(),
            Surface surface => surface.ToBrep(),
            _ => null,
        };
}
