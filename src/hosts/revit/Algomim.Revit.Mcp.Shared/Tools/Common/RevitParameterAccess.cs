using System.Globalization;
using System.Reflection;
using Autodesk.Revit.DB;
using RevitElement = Autodesk.Revit.DB.Element;
using RevitParameter = Autodesk.Revit.DB.Parameter;

namespace Algomim.Revit.Mcp.Tools.Common;

internal static class RevitParameterAccess
{
    public static RevitParameter? FindParameterById(RevitElement element, long parameterId)
        => element.Parameters
            .Cast<RevitParameter>()
            .FirstOrDefault(parameter => parameter.Id.Value == parameterId);

    public static IReadOnlyList<object> ListParameters(RevitElement element)
        => element.Parameters
            .Cast<RevitParameter>()
            .Select(RevitShapes.Parameter)
            .ToList();

    public static bool TrySetParameter(RevitParameter parameter, string rawValue, out string? error)
    {
        error = null;
        if (parameter.IsReadOnly)
        {
            error = "Parameter is read-only.";
            return false;
        }

        try
        {
            switch (parameter.StorageType)
            {
                case StorageType.String:
                    parameter.Set(rawValue);
                    return true;
                case StorageType.Integer:
                    if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
                    {
                        error = "Value must be an integer.";
                        return false;
                    }

                    parameter.Set(integer);
                    return true;
                case StorageType.Double:
                    if (parameter.SetValueString(rawValue))
                        return true;

                    if (!double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                    {
                        error = "Value must be a number or valid unit string.";
                        return false;
                    }

                    parameter.Set(number);
                    return true;
                case StorageType.ElementId:
                    if (!long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                    {
                        error = "Value must be an element id.";
                        return false;
                    }

                    parameter.Set(new ElementId(id));
                    return true;
                default:
                    error = "Unsupported parameter storage type.";
                    return false;
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static IReadOnlyList<object> ListPublicScalarProperties(RevitElement element)
        => element.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(IsReadableScalarProperty)
            .Select(property => new
            {
                name = property.Name,
                type = property.PropertyType.Name,
                canWrite = property.CanWrite,
                value = RevitShapes.PublicProperty(property, element),
            })
            .Cast<object>()
            .ToList();

    public static object? GetPublicProperty(RevitElement element, string propertyName)
    {
        var property = element.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        return property is null || !IsReadableScalarProperty(property) ? null : RevitShapes.PublicProperty(property, element);
    }

    public static bool TrySetPublicProperty(RevitElement element, string propertyName, string rawValue, out string? error)
    {
        error = null;
        var property = element.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property is null)
        {
            error = "Property not found.";
            return false;
        }

        if (!property.CanWrite)
        {
            error = "Property is read-only.";
            return false;
        }

        try
        {
            var value = ConvertScalar(rawValue, property.PropertyType);
            property.SetValue(element, value);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool IsReadableScalarProperty(PropertyInfo property)
        => property.CanRead
           && property.GetIndexParameters().Length == 0
           && (property.PropertyType.IsPrimitive
               || property.PropertyType.IsEnum
               || property.PropertyType == typeof(string)
               || property.PropertyType == typeof(decimal)
               || property.PropertyType == typeof(ElementId)
               || property.PropertyType == typeof(XYZ));

    private static object? ConvertScalar(string rawValue, Type targetType)
    {
        if (targetType == typeof(string)) return rawValue;
        if (targetType == typeof(bool)) return bool.Parse(rawValue);
        if (targetType == typeof(int)) return int.Parse(rawValue, CultureInfo.InvariantCulture);
        if (targetType == typeof(long)) return long.Parse(rawValue, CultureInfo.InvariantCulture);
        if (targetType == typeof(double)) return double.Parse(rawValue, CultureInfo.InvariantCulture);
        if (targetType == typeof(float)) return float.Parse(rawValue, CultureInfo.InvariantCulture);
        if (targetType == typeof(decimal)) return decimal.Parse(rawValue, CultureInfo.InvariantCulture);
        if (targetType == typeof(ElementId)) return new ElementId(long.Parse(rawValue, CultureInfo.InvariantCulture));
        if (targetType.IsEnum) return Enum.Parse(targetType, rawValue, ignoreCase: true);
        return Convert.ChangeType(rawValue, targetType, CultureInfo.InvariantCulture);
    }
}
