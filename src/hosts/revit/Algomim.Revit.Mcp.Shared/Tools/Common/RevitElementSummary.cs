using Autodesk.Revit.DB;
using RevitElement = Autodesk.Revit.DB.Element;
using RevitElementId = Autodesk.Revit.DB.ElementId;
using RevitElementType = Autodesk.Revit.DB.ElementType;

namespace Algomim.Revit.Mcp.Tools.Common;

/// <summary>Stable compact shape for returning Revit elements to MCP clients.</summary>
internal static class RevitElementSummary
{
    public static object FromElement(RevitElement element)
        => new
        {
            id = element.Id.Value,
            uniqueId = element.UniqueId,
            name = SafeName(element),
            className = element.GetType().FullName,
            category = element.Category is null ? null : new
            {
                id = element.Category.Id.Value,
                name = element.Category.Name,
                type = element.Category.CategoryType.ToString(),
            },
            typeId = GetTypeIdValue(element),
            isElementType = element is RevitElementType,
        };

    private static string SafeName(RevitElement element)
    {
        try { return element.Name; }
        catch { return string.Empty; }
    }

    private static long? GetTypeIdValue(RevitElement element)
    {
        try
        {
            var typeId = element.GetTypeId();
            return typeId == RevitElementId.InvalidElementId ? null : typeId.Value;
        }
        catch
        {
            return null;
        }
    }
}
