using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Algomim.Revit.Mcp.Harness;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Composition;
using Algomim.Revit.Mcp.Tools.Common;
using RevitElement = Autodesk.Revit.DB.Element;
using RevitView = Autodesk.Revit.DB.View;

namespace Algomim.Revit.Mcp.Tools.Geometry;

internal static class GeometryToolSet
{
    public static IEnumerable<IMcpTool> Create(RevitToolServices services)
    {
        yield return Tool(services, "geometry_get_locations", "Get point or curve location for element ids.", Schema.From(new { type = "object", properties = new { elementIds = new { type = "array", items = new { type = "integer" } } }, required = new[] { "elementIds" } }), GetLocations);
        yield return Tool(services, "geometry_get_bounding_boxes", "Get bounding boxes for element ids, optionally in a view context.", Schema.From(new { type = "object", properties = new { elementIds = new { type = "array", items = new { type = "integer" } }, viewId = new { type = "integer" } }, required = new[] { "elementIds" } }), GetBoundingBoxes);
        yield return Tool(services, "geometry_get_host_ids", "Get host or tagged element ids for hosted elements and tags.", Schema.From(new { type = "object", properties = new { elementIds = new { type = "array", items = new { type = "integer" } } }, required = new[] { "elementIds" } }), GetHostIds);
        yield return Tool(services, "geometry_get_boundary_lines", "Get boundary or edge lines for walls, rooms, floors and similar elements.", Schema.From(new { type = "object", properties = new { elementIds = new { type = "array", items = new { type = "integer" } } }, required = new[] { "elementIds" } }), GetBoundaryLines);
    }

    private static IMcpTool Tool(RevitToolServices services, string name, string description, System.Text.Json.JsonElement schema, Func<RevitToolContext, ArgumentReader, McpToolResult> execute)
        => new DelegateRevitTool(services.Dispatcher, services.DocumentContextStore, name, description, schema, new ToolMetadata(name, ToolCategory.Geometry, ToolMode.Read, ToolRisk.Low, description), (uiApp, args) =>
        {
            var context = RevitToolBase.TryCreateContext(uiApp, services.DocumentContextStore, out var error);
            return context is null ? error : execute(context, args);
        });

    private static McpToolResult GetLocations(RevitToolContext context, ArgumentReader args)
    {
        var ids = args.RequireLongArray("elementIds", 500);
        var results = ids.Select(id =>
        {
            var element = context.Document.GetElement(RevitIds.Id(id));
            return new
            {
                elementId = id,
                location = ShapeLocation(element?.Location),
            };
        }).ToList();

        return ToolResults.Success(new { count = results.Count, elements = results }, $"{results.Count} location result(s).");
    }

    private static object? ShapeLocation(Location? location)
        => location switch
        {
            LocationPoint point => (object)new { kind = "point", point = RevitShapes.Xyz(point.Point), rotation = (double?)point.Rotation, curve = (object?)null },
            LocationCurve curve => (object)new { kind = "curve", point = (object?)null, rotation = (double?)null, curve = RevitShapes.Curve(curve.Curve) },
            _ => null,
        };

    private static McpToolResult GetBoundingBoxes(RevitToolContext context, ArgumentReader args)
    {
        var ids = args.RequireLongArray("elementIds", 500);
        var viewId = args.OptionalLong("viewId", -1);
        var view = viewId > 0 ? context.Document.GetElement(RevitIds.Id(viewId)) as RevitView : null;
        var results = ids.Select(id =>
        {
            var element = context.Document.GetElement(RevitIds.Id(id));
            return new { elementId = id, boundingBox = element is null ? null : RevitShapes.BoundingBox(element.get_BoundingBox(view)) };
        }).ToList();

        return ToolResults.Success(new { count = results.Count, elements = results }, $"{results.Count} bounding box result(s).");
    }

    private static McpToolResult GetHostIds(RevitToolContext context, ArgumentReader args)
    {
        var ids = args.RequireLongArray("elementIds", 200);
        var results = ids.Select(id =>
        {
            var element = context.Document.GetElement(RevitIds.Id(id));
            return new
            {
                elementId = id,
                hostIds = GetHostIdsForElement(element).Select(hostId => hostId.Value).ToList(),
            };
        }).ToList();

        return ToolResults.Success(new { count = results.Count, elements = results }, $"{results.Count} host/tag result(s).");
    }

    private static McpToolResult GetBoundaryLines(RevitToolContext context, ArgumentReader args)
    {
        var ids = args.RequireLongArray("elementIds", 30);
        var results = ids.Select(id =>
        {
            var element = context.Document.GetElement(RevitIds.Id(id));
            var curves = element is null ? Array.Empty<object>() : GetBoundaryCurves(element).Select(RevitShapes.Curve).ToArray();
            return new { elementId = id, count = curves.Length, lines = curves };
        }).ToList();

        return ToolResults.Success(new { count = results.Count, elements = results }, $"{results.Sum(result => result.count)} boundary line(s) found.");
    }

    private static IEnumerable<ElementId> GetHostIdsForElement(RevitElement? element)
    {
        if (element is null) yield break;

        if (element is FamilyInstance familyInstance && familyInstance.Host is { } host)
            yield return host.Id;

        var taggedIdsMethod = element.GetType().GetMethod("GetTaggedLocalElementIds", Type.EmptyTypes);
        if (taggedIdsMethod?.Invoke(element, null) is IEnumerable<ElementId> taggedIds)
        {
            foreach (var taggedId in taggedIds) yield return taggedId;
        }

        var taggedIdProperty = element.GetType().GetProperty("TaggedLocalElementId");
        if (taggedIdProperty?.GetValue(element) is ElementId taggedElementId && taggedElementId != ElementId.InvalidElementId)
            yield return taggedElementId;
    }

    private static IEnumerable<Curve> GetBoundaryCurves(RevitElement element)
    {
        if (element.Location is LocationCurve locationCurve)
            yield return locationCurve.Curve;

        if (element is Room room)
        {
            foreach (var segmentList in room.GetBoundarySegments(new SpatialElementBoundaryOptions()) ?? new List<IList<BoundarySegment>>())
            {
                foreach (var segment in segmentList)
                    yield return segment.GetCurve();
            }
        }

        var geometry = element.get_Geometry(new Options { DetailLevel = ViewDetailLevel.Fine, IncludeNonVisibleObjects = false });
        if (geometry is null) yield break;

        foreach (var obj in geometry)
        {
            if (obj is Solid solid)
            {
                foreach (Edge edge in solid.Edges)
                    yield return edge.AsCurve();
            }
        }
    }
}
