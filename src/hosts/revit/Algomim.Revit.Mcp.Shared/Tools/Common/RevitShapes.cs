using System.Globalization;
using System.Reflection;
using Autodesk.Revit.DB;
using RevitElement = Autodesk.Revit.DB.Element;
using RevitParameter = Autodesk.Revit.DB.Parameter;

namespace Algomim.Revit.Mcp.Tools.Common;

internal static class RevitShapes
{
    public static object Xyz(XYZ point) => new { x = point.X, y = point.Y, z = point.Z };

    public static object? Color(Color? color)
        => color is null || !color.IsValid ? null : new { red = color.Red, green = color.Green, blue = color.Blue };

    public static object Category(Autodesk.Revit.DB.Category category)
        => new { id = category.Id.Value, name = category.Name, type = category.CategoryType.ToString() };

    public static object? BoundingBox(BoundingBoxXYZ? box)
        => box is null ? null : new { min = Xyz(box.Min), max = Xyz(box.Max), transformOrigin = Xyz(box.Transform.Origin) };

    public static object Curve(Curve curve)
        => new
        {
            curveType = curve.GetType().Name,
            start = Xyz(curve.GetEndPoint(0)),
            end = Xyz(curve.GetEndPoint(1)),
            length = curve.Length,
        };

    public static object ElementRef(RevitElement? element)
        => element is null ? new { id = (long?)null } : RevitElementSummary.FromElement(element);

    public static object Parameter(RevitParameter parameter)
    {
        var value = ParameterValue(parameter);
        return new
        {
            id = parameter.Id.Value,
            name = parameter.Definition?.Name ?? string.Empty,
            storageType = parameter.StorageType.ToString(),
            isReadOnly = parameter.IsReadOnly,
            hasValue = parameter.HasValue,
            value,
            valueString = Safe(() => parameter.AsValueString()),
        };
    }

    public static object? ParameterValue(RevitParameter parameter)
        => parameter.StorageType switch
        {
            StorageType.Integer => parameter.AsInteger(),
            StorageType.Double => parameter.AsDouble(),
            StorageType.String => parameter.AsString(),
            StorageType.ElementId => parameter.AsElementId().Value,
            _ => null,
        };

    public static object OverrideGraphicSettings(OverrideGraphicSettings settings)
        => new
        {
            projectionLineColor = Color(settings.ProjectionLineColor),
            projectionLineWeight = settings.ProjectionLineWeight,
            cutLineColor = Color(settings.CutLineColor),
            cutLineWeight = settings.CutLineWeight,
            surfaceForegroundPatternColor = Color(settings.SurfaceForegroundPatternColor),
            surfaceForegroundPatternId = settings.SurfaceForegroundPatternId.Value,
            surfaceBackgroundPatternColor = Color(settings.SurfaceBackgroundPatternColor),
            surfaceBackgroundPatternId = settings.SurfaceBackgroundPatternId.Value,
            cutForegroundPatternColor = Color(settings.CutForegroundPatternColor),
            cutForegroundPatternId = settings.CutForegroundPatternId.Value,
            cutBackgroundPatternColor = Color(settings.CutBackgroundPatternColor),
            cutBackgroundPatternId = settings.CutBackgroundPatternId.Value,
            transparency = settings.Transparency,
            halftone = settings.Halftone,
        };

    public static object? PublicProperty(PropertyInfo property, object target)
    {
        try
        {
            var value = property.GetValue(target);
            return ShapeValue(value);
        }
        catch
        {
            return null;
        }
    }

    public static object? ShapeValue(object? value)
        => value switch
        {
            null => null,
            string text => text,
            bool boolean => boolean,
            int number => number,
            long number => number,
            double number => number,
            float number => number,
            decimal number => number,
            ElementId id => id.Value,
            XYZ point => Xyz(point),
            RevitElement element => RevitElementSummary.FromElement(element),
            Enum enumValue => enumValue.ToString(),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture),
        };

    private static T? Safe<T>(Func<T?> read)
    {
        try { return read(); }
        catch { return default; }
    }
}
