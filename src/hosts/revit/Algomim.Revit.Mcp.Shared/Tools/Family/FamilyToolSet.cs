using Autodesk.Revit.DB;
using Algomim.Revit.Mcp.Harness;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Composition;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Family;

internal static class FamilyToolSet
{
    public static IEnumerable<IMcpTool> Create(RevitToolServices services)
    {
        yield return Tool(services, "family_list", "List loadable families used in the model.", ToolCategory.Family, Schema.From(new { type = "object", properties = new { } }), FamilyList);
        yield return Tool(services, "family_list_by_category", "List loadable families used in a category.", ToolCategory.Family, Schema.From(new { type = "object", properties = new { categoryId = new { type = "integer" } }, required = new[] { "categoryId" } }), FamilyListByCategory);
        yield return Tool(services, "family_list_types", "List element types for exact family names, including system family names where possible.", ToolCategory.Type, Schema.From(new { type = "object", properties = new { familyNames = new { type = "array", items = new { type = "string" } } }, required = new[] { "familyNames" } }), FamilyListTypes);
        yield return Tool(services, "family_list_elements", "List instance element ids for exact family names.", ToolCategory.Family, Schema.From(new { type = "object", properties = new { familyNames = new { type = "array", items = new { type = "string" } } }, required = new[] { "familyNames" } }), FamilyListElements);
        yield return Tool(services, "type_list_for_elements", "Get type ids and names for element ids.", ToolCategory.Type, Schema.From(new { type = "object", properties = new { elementIds = new { type = "array", items = new { type = "integer" } } }, required = new[] { "elementIds" } }), TypeListForElements);
        yield return Tool(services, "type_list_elements", "List instance element ids for type ids.", ToolCategory.Type, Schema.From(new { type = "object", properties = new { typeIds = new { type = "array", items = new { type = "integer" } } }, required = new[] { "typeIds" } }), TypeListElements);
    }

    private static IMcpTool Tool(RevitToolServices services, string name, string description, ToolCategory category, System.Text.Json.JsonElement schema, Func<RevitToolContext, ArgumentReader, McpToolResult> execute)
        => new DelegateRevitTool(services.Dispatcher, services.DocumentContextStore, name, description, schema, new ToolMetadata(name, category, ToolMode.Read, ToolRisk.Low, description), (uiApp, args) =>
        {
            var context = RevitToolBase.TryCreateContext(uiApp, services.DocumentContextStore, out var error);
            return context is null ? error : execute(context, args);
        });

    private static McpToolResult FamilyList(RevitToolContext context, ArgumentReader args)
    {
        var families = new FilteredElementCollector(context.Document)
            .OfClass(typeof(Autodesk.Revit.DB.Family))
            .Cast<Autodesk.Revit.DB.Family>()
            .OrderBy(family => family.Name)
            .Select(family => ShapeFamily(family))
            .ToList();

        return ToolResults.Success(new { count = families.Count, families }, $"{families.Count} loadable family/families found.");
    }

    private static McpToolResult FamilyListByCategory(RevitToolContext context, ArgumentReader args)
    {
        var categoryId = RevitIds.Id(args.RequireLong("categoryId"));
        var families = new FilteredElementCollector(context.Document)
            .OfClass(typeof(Autodesk.Revit.DB.Family))
            .Cast<Autodesk.Revit.DB.Family>()
            .Where(family => family.FamilyCategory?.Id == categoryId)
            .OrderBy(family => family.Name)
            .Select(family => ShapeFamily(family))
            .ToList();

        return ToolResults.Success(new { count = families.Count, families }, $"{families.Count} family/families found for category.");
    }

    private static McpToolResult FamilyListTypes(RevitToolContext context, ArgumentReader args)
    {
        var names = args.RequireStringArray("familyNames", 100).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var types = new FilteredElementCollector(context.Document)
            .WhereElementIsElementType()
            .OfClass(typeof(ElementType))
            .Cast<ElementType>()
            .Where(type => names.Contains(type.FamilyName))
            .GroupBy(type => type.FamilyName)
            .Select(group => new
            {
                familyName = group.Key,
                count = group.Count(),
                types = group.OrderBy(type => type.Name).Select(ShapeType).ToList(),
            })
            .ToList();

        return ToolResults.Success(new { count = types.Count, families = types }, $"{types.Sum(group => group.count)} type(s) found.");
    }

    private static McpToolResult FamilyListElements(RevitToolContext context, ArgumentReader args)
    {
        var names = args.RequireStringArray("familyNames", 30).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var typeFamilyNames = new FilteredElementCollector(context.Document)
            .WhereElementIsElementType()
            .OfClass(typeof(ElementType))
            .Cast<ElementType>()
            .ToDictionary(type => type.Id, type => type.FamilyName);

        var groups = new FilteredElementCollector(context.Document)
            .WhereElementIsNotElementType()
            .Where(element => typeFamilyNames.TryGetValue(element.GetTypeId(), out var familyName) && names.Contains(familyName))
            .GroupBy(element => typeFamilyNames[element.GetTypeId()])
            .Select(group => new
            {
                familyName = group.Key,
                count = group.Count(),
                elementIds = group.Select(element => element.Id.Value).ToList(),
            })
            .ToList();

        return ToolResults.Success(new { count = groups.Sum(group => group.count), families = groups }, $"{groups.Sum(group => group.count)} element(s) found.");
    }

    private static McpToolResult TypeListForElements(RevitToolContext context, ArgumentReader args)
    {
        var ids = args.RequireLongArray("elementIds", 500);
        var results = ids.Select(id =>
        {
            var element = context.Document.GetElement(RevitIds.Id(id));
            var type = element is null ? null : context.Document.GetElement(element.GetTypeId()) as ElementType;
            return new
            {
                elementId = id,
                type = type is null ? null : ShapeType(type),
            };
        }).ToList();

        return ToolResults.Success(new { count = results.Count, elements = results }, $"{results.Count} element type result(s).");
    }

    private static McpToolResult TypeListElements(RevitToolContext context, ArgumentReader args)
    {
        var typeIds = args.RequireLongArray("typeIds", 50).ToHashSet();
        var groups = new FilteredElementCollector(context.Document)
            .WhereElementIsNotElementType()
            .Where(element => typeIds.Contains(element.GetTypeId().Value))
            .GroupBy(element => element.GetTypeId().Value)
            .Select(group => new
            {
                typeId = group.Key,
                count = group.Count(),
                elementIds = group.Select(element => element.Id.Value).ToList(),
            })
            .ToList();

        return ToolResults.Success(new { count = groups.Sum(group => group.count), types = groups }, $"{groups.Sum(group => group.count)} element(s) found for type ids.");
    }

    private static object ShapeFamily(Autodesk.Revit.DB.Family family)
        => new
        {
            id = family.Id.Value,
            name = family.Name,
            category = family.FamilyCategory is null ? null : RevitShapes.Category(family.FamilyCategory),
            symbolCount = family.GetFamilySymbolIds().Count,
        };

    private static object ShapeType(ElementType type)
        => new
        {
            id = type.Id.Value,
            name = type.Name,
            familyName = type.FamilyName,
            category = type.Category is null ? null : RevitShapes.Category(type.Category),
        };
}
