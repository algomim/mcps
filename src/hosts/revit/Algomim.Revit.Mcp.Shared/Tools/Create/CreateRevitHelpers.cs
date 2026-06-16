using Autodesk.Revit.DB;
using Algomim.Aec.Mcp.Core.Geometry;
using RevitDocument = Autodesk.Revit.DB.Document;

namespace Algomim.Revit.Mcp.Tools.Create;

internal static class CreateRevitHelpers
{
    public static ViewFamilyType FindViewFamilyType(RevitDocument document, ViewFamily family)
        => new FilteredElementCollector(document)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .First(type => type.ViewFamily == family);

    public static XYZ ToXyz(AecPoint3 point)
        => new(point.X, point.Y, point.Z);

    public static BoundingBoxXYZ SectionBox(AecPoint3 start, AecPoint3 end, double depth, double height)
    {
        var startXyz = ToXyz(start);
        var endXyz = ToXyz(end);
        var direction = (endXyz - startXyz).Normalize();
        var up = XYZ.BasisZ;
        var viewDirection = direction.CrossProduct(up).Normalize();
        var midpoint = (startXyz + endXyz) / 2;
        var width = startXyz.DistanceTo(endXyz);
        var transform = Transform.Identity;
        transform.Origin = midpoint;
        transform.BasisX = direction;
        transform.BasisY = up;
        transform.BasisZ = viewDirection;

        return new BoundingBoxXYZ
        {
            Transform = transform,
            Min = new XYZ(-width / 2, 0, 0),
            Max = new XYZ(width / 2, height, depth),
        };
    }
}
