using Autodesk.Revit.DB;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Composition;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Parameter.ParameterValuesSet;
using Algomim.Revit.Mcp.Tools.Parameter.PropertyValuesSet;

namespace Algomim.Revit.Mcp.Tools.Parameter;

internal static class ParameterToolSet
{
    public static IEnumerable<IMcpTool> Create(RevitToolServices services)
    {
        yield return Read(services, "parameter_list", "List parameters for one element or type id.", Schema.From(new { type = "object", properties = new { elementId = new { type = "integer" } }, required = new[] { "elementId" } }), ParameterList);
        yield return Read(services, "parameter_get_values", "Get one parameter value for many element/type ids.", Schema.From(new { type = "object", properties = new { elementIds = new { type = "array", items = new { type = "integer" } }, parameterId = new { type = "integer" } }, required = new[] { "elementIds", "parameterId" } }), ParameterGetValues);
        yield return ParameterValuesSetTool.Create(services);
        yield return Read(services, "property_list", "List scalar public Revit API properties for one element.", Schema.From(new { type = "object", properties = new { elementId = new { type = "integer" } }, required = new[] { "elementId" } }), PropertyList);
        yield return Read(services, "property_get_values", "Get one scalar public property value for many elements.", Schema.From(new { type = "object", properties = new { propertyName = new { type = "string" }, elementIds = new { type = "array", items = new { type = "integer" } } }, required = new[] { "propertyName", "elementIds" } }), PropertyGetValues);
        yield return PropertyValuesSetTool.Create(services);
        yield return Read(services, "material_get_layers", "Get compound structure material layers for host object type ids.", Schema.From(new { type = "object", properties = new { typeIds = new { type = "array", items = new { type = "integer" } } }, required = new[] { "typeIds" } }), MaterialGetLayers);
    }

    private static IMcpTool Read(RevitToolServices services, string name, string description, System.Text.Json.JsonElement schema, Func<RevitToolContext, ArgumentReader, McpToolResult> execute)
        => Tool(services, name, description, schema, ToolMode.Read, ToolRisk.Low, execute);

    private static IMcpTool Tool(RevitToolServices services, string name, string description, System.Text.Json.JsonElement schema, ToolMode mode, ToolRisk risk, Func<RevitToolContext, ArgumentReader, McpToolResult> execute)
        => new DelegateRevitTool(services.Dispatcher, services.DocumentContextStore, name, description, schema, new ToolMetadata(name, ToolCategory.Parameter, mode, risk, description), (uiApp, args) =>
        {
            var context = RevitToolBase.TryCreateContext(uiApp, services.DocumentContextStore, out var error);
            return context is null ? error : execute(context, args);
        });

    private static McpToolResult ParameterList(RevitToolContext context, ArgumentReader args)
    {
        var element = context.Document.GetElement(RevitIds.Id(args.RequireLong("elementId")));
        if (element is null) return ToolResults.Error("ELEMENT_NOT_FOUND", "Element not found.");

        var parameters = RevitParameterAccess.ListParameters(element);
        return ToolResults.Success(new { element = RevitElementSummary.FromElement(element), count = parameters.Count, parameters }, $"{parameters.Count} parameter(s) found.");
    }

    private static McpToolResult ParameterGetValues(RevitToolContext context, ArgumentReader args)
    {
        var elementIds = args.RequireLongArray("elementIds", 500);
        var parameterId = args.RequireLong("parameterId");
        var values = elementIds.Select(id =>
        {
            var element = context.Document.GetElement(RevitIds.Id(id));
            var parameter = element is null ? null : RevitParameterAccess.FindParameterById(element, parameterId);
            return new
            {
                elementId = id,
                found = parameter is not null,
                value = parameter is null ? null : RevitShapes.ParameterValue(parameter),
                valueString = parameter?.AsValueString(),
            };
        }).ToList();

        return ToolResults.Success(new { count = values.Count, values }, $"{values.Count} parameter value result(s).");
    }

    private static McpToolResult PropertyList(RevitToolContext context, ArgumentReader args)
    {
        var element = context.Document.GetElement(RevitIds.Id(args.RequireLong("elementId")));
        if (element is null) return ToolResults.Error("ELEMENT_NOT_FOUND", "Element not found.");

        var properties = RevitParameterAccess.ListPublicScalarProperties(element);
        return ToolResults.Success(new { element = RevitElementSummary.FromElement(element), count = properties.Count, properties }, $"{properties.Count} scalar propertie(s) found.");
    }

    private static McpToolResult PropertyGetValues(RevitToolContext context, ArgumentReader args)
    {
        var propertyName = args.RequireString("propertyName");
        var elementIds = args.RequireLongArray("elementIds", 500);
        var values = elementIds.Select(id =>
        {
            var element = context.Document.GetElement(RevitIds.Id(id));
            return new { elementId = id, value = element is null ? null : RevitParameterAccess.GetPublicProperty(element, propertyName) };
        }).ToList();

        return ToolResults.Success(new { count = values.Count, values }, $"{values.Count} property value result(s).");
    }

    private static McpToolResult MaterialGetLayers(RevitToolContext context, ArgumentReader args)
    {
        var ids = args.RequireLongArray("typeIds", 100);
        var results = ids.Select(id =>
        {
            var element = context.Document.GetElement(RevitIds.Id(id));
            var hostType = element as HostObjAttributes;
            var structure = hostType?.GetCompoundStructure();
            var layers = structure?.GetLayers()
                .Select((layer, index) => new
                {
                    index,
                    function = layer.Function.ToString(),
                    width = layer.Width,
                    materialId = layer.MaterialId.Value,
                    materialName = context.Document.GetElement(layer.MaterialId)?.Name,
                })
                .ToList();

            return new
            {
                typeId = id,
                typeName = element?.Name,
                hasCompoundStructure = structure is not null,
                layerCount = layers?.Count ?? 0,
                layers,
            };
        }).ToList();

        return ToolResults.Success(new { count = results.Count, types = results }, $"{results.Count} material layer result(s).");
    }
}
