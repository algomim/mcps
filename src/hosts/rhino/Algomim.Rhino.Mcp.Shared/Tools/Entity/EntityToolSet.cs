using Algomim.Aec.Mcp.Tooling;
using Algomim.Rhino.Mcp.Tools.Common;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace Algomim.Rhino.Mcp.Tools.Entity;

internal static class EntityToolSet
{
    private const int DefaultLimit = 100;
    private const int MaxLimit = 500;

    public static IEnumerable<IMcpTool> Create()
    {
        yield return List();
        yield return GetInfo();
        yield return Analyze();
        yield return GetAttributes();
        yield return Delete();
        yield return Move();
        yield return Rotate();
        yield return Scale();
        yield return SetAttributes();
        yield return UpdateAttributes();
    }

    private static IMcpTool List()
        => new DelegateRhinoTool(
            "entity_query",
            "Lists Rhino document objects by optional name, layer, geometry type, and visibility filters.",
            RhinoSchemas.Object(new Dictionary<string, object>(RhinoSchemas.Props(
                ("layer", "string", "Layer full path or name filter"),
                ("geometryType", "string", "Geometry filter: point, pointset, curve, surface, brep, mesh, annotation, light, block"),
                ("includeHidden", "boolean", "Include hidden objects"),
                ("includeLocked", "boolean", "Include locked objects")))
            {
                ["names"] = RhinoSchemas.StringArray("Exact object names to match"),
                ["limit"] = RhinoSchemas.Integer("Maximum number of objects to return", 1, MaxLimit, DefaultLimit),
            }),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var warnings = new List<string>();
                var names = reader.GetStringArray("names").ToHashSet(StringComparer.Ordinal);
                var layer = reader.GetString("layer");
                var geometryType = reader.GetString("geometryType");
                var limit = Math.Clamp(reader.GetInt("limit", DefaultLimit), 1, MaxLimit);

                var settings = RhinoObjectFilters.ActiveObjects(
                    includeHidden: reader.GetBool("includeHidden"),
                    includeLocked: reader.GetBool("includeLocked", fallback: true));

                if (!RhinoObjectFilters.TryApplyGeometryType(settings, geometryType))
                    throw new ArgumentException("Argument 'geometryType' must be one of: point, pointset, curve, surface, brep, mesh, annotation, light, block.");

                if (!string.IsNullOrWhiteSpace(layer))
                {
                    var layerIndex = RhinoObjectFilters.FindLayerIndex(document, layer);
                    if (layerIndex < 0)
                    {
                        warnings.Add($"Layer not found: {layer}");
                        return ToolResponse.Success(
                            new { count = 0, truncated = false, objects = Array.Empty<object>() },
                            "Found 0 Rhino objects.",
                            warnings);
                    }

                    settings.LayerIndexFilter = layerIndex;
                }

                var matches = document.Objects.GetObjectList(settings)
                    .Where(obj => names.Count == 0 || names.Contains(obj.Name ?? string.Empty))
                    .Take(limit + 1)
                    .Select(obj => RhinoObjectSummary.From(document, obj))
                    .ToArray();
                var truncated = matches.Length > limit;
                var objects = truncated ? matches.Take(limit).ToArray() : matches;

                var data = new
                {
                    count = objects.Length,
                    truncated,
                    objects,
                };
                return ToolResponse.Success(data, $"Found {objects.Length} Rhino object(s).", warnings);
            }));

    private static IMcpTool Analyze()
        => new DelegateRhinoTool(
            "entity_analyze",
            "Analyzes Rhino objects for validity, bounding boxes, and curve/surface/solid/mesh metrics.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["ids"] = RhinoSchemas.StringArray("Object GUIDs. Defaults to selected objects when omitted."),
                ["selected"] = RhinoSchemas.Boolean("Analyze the current selection when ids are omitted", defaultValue: false),
            }),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var ids = reader.GetStringArray("ids")
                    .Select(value =>
                    {
                        if (!Guid.TryParse(value, out var guid))
                            throw new ArgumentException($"Argument 'ids' contains an invalid GUID: {value}");
                        return guid;
                    })
                    .ToArray();

                var objects = ids.Length > 0
                    ? ids.Select(id => RhinoIds.RequireObject(document, id)).ToArray()
                    : document.Objects.GetSelectedObjects(false, false).ToArray();

                if (objects.Length == 0)
                    throw new ArgumentException("No object IDs were provided and no Rhino objects are selected.");

                var analyses = objects.Select(obj => AnalyzeObject(document, obj)).ToArray();
                return ToolResponse.Success(new { count = analyses.Length, objects = analyses }, $"Analyzed {analyses.Length} Rhino object(s).");
            }));

    private static IMcpTool GetAttributes()
        => new DelegateRhinoTool(
            "entity_get_attributes",
            "Gets mutable Rhino object attributes: name, layer, color, material, visibility, lock state, and user strings.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["id"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Object GUID" },
            }, ["id"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var obj = RhinoIds.RequireObject(document, new ArgumentReader(args).RequireGuid("id"));
                return ToolResponse.Success(AttributeSummary(document, obj), $"Read attributes for Rhino object {obj.Id:D}.");
            }));

    private static IMcpTool GetInfo()
        => new DelegateRhinoTool(
            "entity_get_info",
            "Gets core Rhino object information by GUID.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["id"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Object GUID" },
            }, ["id"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var id = new ArgumentReader(args).RequireGuid("id");
                var obj = RhinoIds.RequireObject(document, id);
                return ToolResponse.Success(RhinoObjectSummary.From(document, obj), $"Read Rhino object {id:D}.");
            }));

    private static IMcpTool Delete()
        => new DelegateRhinoTool(
            "entity_delete",
            "Deletes Rhino objects by GUID.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["ids"] = RhinoSchemas.StringArray("Object GUIDs"),
            }, ["ids"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var ids = new ArgumentReader(args).RequireGuidArray("ids");
                var deleted = 0;
                foreach (var id in ids)
                {
                    var obj = document.Objects.FindId(id);
                    if (obj is not null && document.Objects.Delete(obj, true))
                        deleted++;
                }

                document.Views.Redraw();
                return ToolResponse.Success(new { deleted }, $"Deleted {deleted} Rhino object(s).");
            }));

    private static IMcpTool Move()
        => TransformTool(
            "entity_move",
            "Moves Rhino objects by a vector.",
            new Dictionary<string, object>
            {
                ["translation"] = RhinoSchemas.Point("Move vector"),
            },
            ["ids", "translation"],
            (reader, _) => Transform.Translation(reader.RequireVector("translation")),
            "Moved");

    private static IMcpTool Rotate()
        => TransformTool(
            "entity_rotate",
            "Rotates Rhino objects around an axis and center point.",
            new Dictionary<string, object>
            {
                ["center"] = RhinoSchemas.Point("Rotation center; defaults to each object's bounding-box center"),
                ["axis"] = RhinoSchemas.Point("Rotation axis vector; defaults to world Z"),
                ["angle"] = RhinoSchemas.Number("Rotation angle in degrees"),
            },
            ["ids", "angle"],
            (reader, _) =>
            {
                var axis = reader.GetVector("axis", Vector3d.ZAxis);
                if (!axis.Unitize())
                    throw new ArgumentException("Argument 'axis' must be a non-zero vector.");

                return Transform.Rotation(DegreesToRadians(reader.RequireDouble("angle")), axis, ObjectCenter(_, reader));
            },
            "Rotated");

    private static IMcpTool Scale()
        => TransformTool(
            "entity_scale",
            "Scales Rhino objects uniformly around a center point.",
            new Dictionary<string, object>
            {
                ["center"] = RhinoSchemas.Point("Scale center; defaults to each object's bounding-box center"),
                ["factor"] = RhinoSchemas.Number("Uniform scale factor", minimum: 0),
            },
            ["ids", "factor"],
            (reader, obj) =>
            {
                var factor = reader.RequireDouble("factor");
                if (factor <= 0)
                    throw new ArgumentException("Argument 'factor' must be positive.");

                return Transform.Scale(ObjectCenter(obj, reader), factor);
            },
            "Scaled");

    private static IMcpTool SetAttributes()
        => new DelegateRhinoTool(
            "entity_set_attributes",
            "Sets Rhino object name, layer, and object color.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["ids"] = RhinoSchemas.StringArray("Object GUIDs"),
                ["name"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "New object name" },
                ["layer"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Target layer name" },
                ["color"] = RhinoSchemas.Color("Object color as RGB"),
            }, ["ids"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var updated = 0;
                foreach (var id in reader.RequireGuidArray("ids"))
                {
                    var obj = RhinoIds.RequireObject(document, id);
                    RhinoAttributes.ApplyMutableAttributes(document, obj, reader);
                    updated++;
                }

                document.Views.Redraw();
                return ToolResponse.Success(new { updated }, $"Updated attributes on {updated} Rhino object(s).");
            }));

    private static IMcpTool UpdateAttributes()
        => new DelegateRhinoTool(
            "entity_update_attributes",
            "Updates Rhino object attributes including visibility, lock state, material source, and user strings.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["id"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Object GUID" },
                ["name"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "New object name" },
                ["layer"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Target layer name" },
                ["color"] = RhinoSchemas.Color("Object color as RGB"),
                ["materialIndex"] = new Dictionary<string, object> { ["type"] = "integer", ["description"] = "Material table index; -1 restores layer material" },
                ["visible"] = RhinoSchemas.Boolean("Object visibility"),
                ["locked"] = RhinoSchemas.Boolean("Object lock state"),
                ["userStrings"] = RhinoSchemas.JsonObject("User string key/value updates. Null deletes a key."),
                ["deleteUserStrings"] = RhinoSchemas.StringArray("User string keys to delete"),
                ["clearUserStrings"] = RhinoSchemas.Boolean("Delete all user strings", defaultValue: false),
            }, ["id"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var obj = RhinoIds.RequireObject(document, reader.RequireGuid("id"));
                var attrs = obj.Attributes.Duplicate();
                var modified = false;

                if (reader.Has("name"))
                {
                    attrs.Name = reader.GetString("name");
                    modified = true;
                }

                if (reader.Has("layer"))
                {
                    var layerName = reader.RequireString("layer");
                    var layerIndex = RhinoObjectFilters.FindLayerIndex(document, layerName);
                    if (layerIndex < 0)
                        throw new ArgumentException($"Layer not found: {layerName}");

                    attrs.LayerIndex = layerIndex;
                    modified = true;
                }

                var color = reader.GetColor("color");
                if (color.HasValue)
                {
                    attrs.ObjectColor = color.Value;
                    attrs.ColorSource = ObjectColorSource.ColorFromObject;
                    modified = true;
                }

                if (reader.Has("materialIndex"))
                {
                    var materialIndex = reader.GetInt("materialIndex", -1);
                    if (materialIndex < -1 || materialIndex >= document.Materials.Count)
                        throw new ArgumentException($"Material index {materialIndex} is outside the document material table.");

                    attrs.MaterialIndex = materialIndex;
                    attrs.MaterialSource = materialIndex == -1
                        ? ObjectMaterialSource.MaterialFromLayer
                        : ObjectMaterialSource.MaterialFromObject;
                    modified = true;
                }

                if (reader.GetBool("clearUserStrings"))
                {
                    foreach (var key in attrs.GetUserStrings().AllKeys.Where(key => !string.IsNullOrEmpty(key)))
                    {
                        attrs.SetUserString(key, null);
                    }
                    modified = true;
                }

                foreach (var key in reader.GetStringArray("deleteUserStrings"))
                {
                    attrs.SetUserString(key, null);
                    modified = true;
                }

                var userStrings = reader.GetElement("userStrings");
                if (userStrings is { ValueKind: System.Text.Json.JsonValueKind.Object })
                {
                    foreach (var property in userStrings.Value.EnumerateObject())
                    {
                        attrs.SetUserString(
                            property.Name,
                            property.Value.ValueKind == System.Text.Json.JsonValueKind.Null ? null : property.Value.ToString());
                        modified = true;
                    }
                }

                if (modified)
                    document.Objects.ModifyAttributes(obj, attrs, true);

                if (reader.Has("locked") && !reader.GetBool("locked"))
                    document.Objects.Unlock(obj.Id, false);
                if (reader.Has("visible") && reader.GetBool("visible"))
                    document.Objects.Show(obj.Id, false);
                if (reader.Has("visible") && !reader.GetBool("visible"))
                    document.Objects.Hide(obj.Id, false);
                if (reader.Has("locked") && reader.GetBool("locked"))
                {
                    document.Objects.Show(obj.Id, false);
                    document.Objects.Lock(obj.Id, false);
                }

                document.Views.Redraw();
                var updated = RhinoIds.RequireObject(document, obj.Id);
                return ToolResponse.Success(AttributeSummary(document, updated), $"Updated attributes for Rhino object {obj.Id:D}.");
            }));

    private static IMcpTool TransformTool(
        string name,
        string description,
        Dictionary<string, object> extraProperties,
        IReadOnlyList<string> required,
        Func<ArgumentReader, global::Rhino.DocObjects.RhinoObject, Transform> transformFactory,
        string summaryVerb)
    {
        var properties = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["ids"] = RhinoSchemas.StringArray("Object GUIDs"),
        };
        foreach (var property in extraProperties)
            properties[property.Key] = property.Value;

        return new DelegateRhinoTool(
            name,
            description,
            RhinoSchemas.Object(properties, required),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var ids = reader.RequireGuidArray("ids");
                var updated = 0;
                foreach (var id in ids)
                {
                    var obj = RhinoIds.RequireObject(document, id);
                    if (document.Objects.Transform(obj, transformFactory(reader, obj), true) != Guid.Empty)
                        updated++;
                }

                document.Views.Redraw();
                return ToolResponse.Success(new { updated }, $"{summaryVerb} {updated} Rhino object(s).");
            }));
    }

    private static Point3d ObjectCenter(global::Rhino.DocObjects.RhinoObject obj, ArgumentReader reader)
        => reader.Has("center")
            ? reader.GetPoint("center", Point3d.Origin)
            : obj.Geometry.GetBoundingBox(true).Center;

    private static object AttributeSummary(global::Rhino.RhinoDoc document, global::Rhino.DocObjects.RhinoObject obj)
    {
        var attrs = obj.Attributes;
        var layer = attrs.LayerIndex >= 0 ? document.Layers[attrs.LayerIndex] : null;
        return new
        {
            id = obj.Id.ToString("D"),
            name = obj.Name,
            type = obj.ObjectType.ToString(),
            layer = layer is null ? null : new { layer.Index, layer.Id, layer.Name, layer.FullPath },
            color = new { attrs.ObjectColor.R, attrs.ObjectColor.G, attrs.ObjectColor.B },
            colorSource = attrs.ColorSource.ToString(),
            materialIndex = attrs.MaterialIndex,
            materialSource = attrs.MaterialSource.ToString(),
            visible = obj.Visible,
            hidden = obj.IsHidden,
            locked = obj.IsLocked,
            userStrings = attrs.GetUserStrings().AllKeys
                .Where(key => !string.IsNullOrEmpty(key))
                .ToDictionary(key => key!, key => attrs.GetUserString(key!), StringComparer.Ordinal),
        };
    }

    private static object AnalyzeObject(global::Rhino.RhinoDoc document, global::Rhino.DocObjects.RhinoObject obj)
    {
        var geometry = obj.Geometry;
        var box = geometry.GetBoundingBox(true);
        var valid = geometry.IsValid;
        var validityLog = string.Empty;
        try
        {
            valid = geometry.IsValidWithLog(out validityLog);
        }
        catch
        {
            // Some Rhino geometry implementations do not surface a validity log.
        }

        return new
        {
            summary = RhinoObjectSummary.From(document, obj),
            valid,
            validityLog = string.IsNullOrWhiteSpace(validityLog) ? null : validityLog,
            boundingBox = box.IsValid ? new { min = Point(box.Min), max = Point(box.Max), center = Point(box.Center) } : null,
            metrics = GeometryMetrics(geometry),
        };
    }

    private static object GeometryMetrics(GeometryBase geometry)
    {
        if (geometry is global::Rhino.Geometry.Point point)
            return new { location = Point(point.Location) };

        if (geometry is Curve curve)
        {
            var area = AreaMassProperties.Compute(curve);
            return new
            {
                kind = "curve",
                length = curve.GetLength(),
                curve.IsClosed,
                curve.IsPeriodic,
                curve.Degree,
                curve.SpanCount,
                start = Point(curve.PointAtStart),
                end = Point(curve.PointAtEnd),
                area = area?.Area,
                centroid = area is null ? null : Point(area.Centroid),
            };
        }

        if (geometry is Extrusion extrusion)
            geometry = extrusion.ToBrep();
        if (geometry is Surface surface)
            geometry = surface.ToBrep();

        if (geometry is Brep brep)
        {
            var area = AreaMassProperties.Compute(brep);
            var volume = VolumeMassProperties.Compute(brep);
            return new
            {
                kind = "brep",
                brep.IsSolid,
                faceCount = brep.Faces.Count,
                edgeCount = brep.Edges.Count,
                vertexCount = brep.Vertices.Count,
                nakedEdgeCount = brep.DuplicateNakedEdgeCurves(true, false)?.Length ?? 0,
                area = area?.Area,
                areaCentroid = area is null ? null : Point(area.Centroid),
                volume = volume?.Volume,
                volumeCentroid = volume is null ? null : Point(volume.Centroid),
            };
        }

        if (geometry is Mesh mesh)
        {
            var area = AreaMassProperties.Compute(mesh);
            var volume = VolumeMassProperties.Compute(mesh);
            return new
            {
                kind = "mesh",
                mesh.IsClosed,
                faceCount = mesh.Faces.Count,
                vertexCount = mesh.Vertices.Count,
                edgeCount = mesh.TopologyEdges.Count,
                nakedEdgeCount = mesh.GetNakedEdges()?.Length ?? 0,
                area = area?.Area,
                areaCentroid = area is null ? null : Point(area.Centroid),
                volume = volume?.Volume,
                volumeCentroid = volume is null ? null : Point(volume.Centroid),
            };
        }

        return new { kind = geometry.GetType().Name };
    }

    private static object Point(Point3d point)
        => new { x = point.X, y = point.Y, z = point.Z };

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
