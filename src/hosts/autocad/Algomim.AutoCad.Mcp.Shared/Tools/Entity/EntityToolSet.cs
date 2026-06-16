using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Algomim.Aec.Mcp.Tooling;
using Algomim.AutoCad.Mcp.Harness;
using Algomim.AutoCad.Mcp.Tools.Common;

namespace Algomim.AutoCad.Mcp.Tools.Entity;

internal static class EntityToolSet
{
    public static IEnumerable<IMcpTool> Create(AutoCadToolServices services)
    {
        yield return Query(services);
        yield return GetProperties(services);
        yield return Delete(services);
        yield return Move(services);
        yield return Rotate(services);
        yield return Scale(services);
        yield return Copy(services);
        yield return Mirror(services);
        yield return Offset(services);
        yield return SetLayer(services);
        yield return SetColor(services);
    }

    private static IMcpTool Query(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "entity_query",
            "Queries model space entities by optional AutoCAD type and layer.",
            AutoCadSchemas.Object(
                AutoCadSchemas.Props(
                    ("type", "string", "Entity type filter, for example Line or Circle"),
                    ("layer", "string", "Layer filter"),
                    ("limit", "number", "Maximum result count"))),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var typeFilter = reader.GetString("type");
                var layerFilter = reader.GetString("layer");
                var limit = Math.Max(1, reader.GetInt("limit", 100));

                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForRead);
                    var entities = new List<object>();

                    foreach (ObjectId id in model)
                    {
                        if (transaction.GetObject(id, OpenMode.ForRead) is not Autodesk.AutoCAD.DatabaseServices.Entity entity)
                            continue;

                        var type = AutoCadDb.EntityType(entity);
                        if (!string.IsNullOrWhiteSpace(typeFilter) &&
                            !string.Equals(type, typeFilter, StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (!string.IsNullOrWhiteSpace(layerFilter) &&
                            !string.Equals(entity.Layer, layerFilter, StringComparison.OrdinalIgnoreCase))
                            continue;

                        entities.Add(AutoCadDb.EntitySummary(entity));
                        if (entities.Count >= limit) break;
                    }

                    return new { count = entities.Count, entities };
                });

                return ToolResponse.Success(data, $"Found {data.count} entities.");
            }));

    private static IMcpTool GetProperties(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "entity_get_properties",
            "Gets core properties for an entity handle.",
            AutoCadSchemas.Object(AutoCadSchemas.Props(("handle", "string", "Entity handle")), ["handle"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var handle = new ArgumentReader(args).RequireString("handle");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    var entity = (Autodesk.AutoCAD.DatabaseServices.Entity)transaction.GetObject(AutoCadDb.ObjectIdFromHandle(database, handle), OpenMode.ForRead);
                    return AutoCadDb.EntitySummary(entity);
                });
                return ToolResponse.Success(data, $"Read entity {handle}.");
            }));

    private static IMcpTool Delete(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "entity_delete",
            "Deletes entities by handle.",
            HandlesSchema(),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var handles = new ArgumentReader(args).RequireStringArray("handles");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    var count = 0;
                    foreach (var handle in handles)
                    {
                        if (transaction.GetObject(AutoCadDb.ObjectIdFromHandle(database, handle), OpenMode.ForWrite) is DBObject obj)
                        {
                            obj.Erase();
                            count++;
                        }
                    }

                    return new { deleted = count };
                });
                return ToolResponse.Success(data, $"Deleted {data.deleted} entities.");
            }));

    private static IMcpTool Move(AutoCadToolServices services)
        => TransformTool(
            services,
            "entity_move",
            "Moves entities by a delta vector.",
            AutoCadSchemas.Props(
                ("dx", "number", "Delta X"),
                ("dy", "number", "Delta Y"),
                ("dz", "number", "Delta Z")),
            ["handles", "dx", "dy"],
            reader => Matrix3d.Displacement(new Vector3d(reader.RequireDouble("dx"), reader.RequireDouble("dy"), reader.GetDouble("dz"))),
            "Moved");

    private static IMcpTool Rotate(AutoCadToolServices services)
        => TransformTool(
            services,
            "entity_rotate",
            "Rotates entities around a base point by degrees.",
            AutoCadSchemas.Props(
                ("baseX", "number", "Base point X"),
                ("baseY", "number", "Base point Y"),
                ("baseZ", "number", "Base point Z"),
                ("angle", "number", "Rotation angle in degrees")),
            ["handles", "baseX", "baseY", "angle"],
            reader => Matrix3d.Rotation(
                DegreesToRadians(reader.RequireDouble("angle")),
                Vector3d.ZAxis,
                AutoCadDb.Point(reader.RequireDouble("baseX"), reader.RequireDouble("baseY"), reader.GetDouble("baseZ"))),
            "Rotated");

    private static IMcpTool Scale(AutoCadToolServices services)
        => TransformTool(
            services,
            "entity_scale",
            "Scales entities from a base point.",
            AutoCadSchemas.Props(
                ("baseX", "number", "Base point X"),
                ("baseY", "number", "Base point Y"),
                ("baseZ", "number", "Base point Z"),
                ("factor", "number", "Scale factor")),
            ["handles", "baseX", "baseY", "factor"],
            reader => Matrix3d.Scaling(
                reader.RequireDouble("factor"),
                AutoCadDb.Point(reader.RequireDouble("baseX"), reader.RequireDouble("baseY"), reader.GetDouble("baseZ"))),
            "Scaled");

    private static IMcpTool Mirror(AutoCadToolServices services)
        => TransformTool(
            services,
            "entity_mirror",
            "Mirrors entities across an axis line.",
            AutoCadSchemas.Props(
                ("x1", "number", "Axis start X"),
                ("y1", "number", "Axis start Y"),
                ("x2", "number", "Axis end X"),
                ("y2", "number", "Axis end Y")),
            ["handles", "x1", "y1", "x2", "y2"],
            reader => Matrix3d.Mirroring(new Line3d(
                AutoCadDb.Point(reader.RequireDouble("x1"), reader.RequireDouble("y1")),
                AutoCadDb.Point(reader.RequireDouble("x2"), reader.RequireDouble("y2")))),
            "Mirrored");

    private static IMcpTool Copy(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "entity_copy",
            "Copies entities by a delta vector.",
            HandlesSchema(
                AutoCadSchemas.Props(
                    ("dx", "number", "Delta X"),
                    ("dy", "number", "Delta Y"),
                    ("dz", "number", "Delta Z")),
                ["handles", "dx", "dy"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var handles = reader.RequireStringArray("handles");
                var matrix = Matrix3d.Displacement(new Vector3d(reader.RequireDouble("dx"), reader.RequireDouble("dy"), reader.GetDouble("dz")));
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForWrite);
                    var created = new List<object>();
                    foreach (var handle in handles)
                    {
                        var source = transaction.GetObject(AutoCadDb.ObjectIdFromHandle(database, handle), OpenMode.ForRead) as Autodesk.AutoCAD.DatabaseServices.Entity;
                        if (source is null) continue;

                        var clone = (Autodesk.AutoCAD.DatabaseServices.Entity)source.Clone();
                        clone.TransformBy(matrix);
                        AutoCadDb.AddEntity(model, transaction, clone);
                        created.Add(AutoCadDb.EntitySummary(clone));
                    }

                    return new { count = created.Count, entities = created };
                });
                return ToolResponse.Success(data, $"Copied {data.count} entities.");
            }));

    private static IMcpTool Offset(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "entity_offset",
            "Offsets a curve entity by distance.",
            AutoCadSchemas.Object(
                AutoCadSchemas.Props(
                    ("handle", "string", "Curve entity handle"),
                    ("distance", "number", "Offset distance")),
                ["handle", "distance"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var handle = reader.RequireString("handle");
                var distance = reader.RequireDouble("distance");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    var curve = transaction.GetObject(AutoCadDb.ObjectIdFromHandle(database, handle), OpenMode.ForRead) as Curve;
                    if (curve is null)
                        throw new ArgumentException("Entity is not a curve.");

                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForWrite);
                    var offsets = curve.GetOffsetCurves(distance);
                    var created = new List<object>();
                    foreach (DBObject obj in offsets)
                    {
                        if (obj is not Autodesk.AutoCAD.DatabaseServices.Entity entity) continue;
                        AutoCadDb.AddEntity(model, transaction, entity);
                        created.Add(AutoCadDb.EntitySummary(entity));
                    }

                    return new { count = created.Count, entities = created };
                });
                return ToolResponse.Success(data, $"Created {data.count} offset entities.");
            }));

    private static IMcpTool SetLayer(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "entity_set_layer",
            "Moves entities to a target layer.",
            HandlesSchema(AutoCadSchemas.Props(("layer", "string", "Target layer name")), ["handles", "layer"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var handles = reader.RequireStringArray("handles");
                var layer = reader.RequireString("layer");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    AutoCadDb.EnsureLayer(database, transaction, layer);
                    var count = 0;
                    foreach (var handle in handles)
                    {
                        if (transaction.GetObject(AutoCadDb.ObjectIdFromHandle(database, handle), OpenMode.ForWrite) is Autodesk.AutoCAD.DatabaseServices.Entity entity)
                        {
                            entity.Layer = layer;
                            count++;
                        }
                    }

                    return new { updated = count, layer };
                });
                return ToolResponse.Success(data, $"Moved {data.updated} entities to layer '{layer}'.");
            }));

    private static IMcpTool SetColor(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "entity_set_color",
            "Sets entities to an ACI color index.",
            HandlesSchema(AutoCadSchemas.Props(("colorIndex", "number", "ACI color index")), ["handles", "colorIndex"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var handles = reader.RequireStringArray("handles");
                var colorIndex = reader.GetInt("colorIndex", 7);
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    var count = 0;
                    foreach (var handle in handles)
                    {
                        if (transaction.GetObject(AutoCadDb.ObjectIdFromHandle(database, handle), OpenMode.ForWrite) is Autodesk.AutoCAD.DatabaseServices.Entity entity)
                        {
                            entity.Color = Color.FromColorIndex(ColorMethod.ByAci, Convert.ToInt16(colorIndex));
                            count++;
                        }
                    }

                    return new { updated = count, colorIndex };
                });
                return ToolResponse.Success(data, $"Updated color on {data.updated} entities.");
            }));

    private static IMcpTool TransformTool(
        AutoCadToolServices services,
        string name,
        string description,
        Dictionary<string, object> properties,
        IReadOnlyList<string> required,
        Func<ArgumentReader, Matrix3d> matrixFactory,
        string verb)
        => new DelegateAutoCadTool(
            name,
            description,
            HandlesSchema(properties, required),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var handles = reader.RequireStringArray("handles");
                var matrix = matrixFactory(reader);
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    var count = 0;
                    foreach (var handle in handles)
                    {
                        if (transaction.GetObject(AutoCadDb.ObjectIdFromHandle(database, handle), OpenMode.ForWrite) is Autodesk.AutoCAD.DatabaseServices.Entity entity)
                        {
                            entity.TransformBy(matrix);
                            count++;
                        }
                    }

                    return new { updated = count };
                });
                return ToolResponse.Success(data, $"{verb} {data.updated} entities.");
            }));

    private static System.Text.Json.JsonElement HandlesSchema(
        Dictionary<string, object>? extraProperties = null,
        IReadOnlyList<string>? required = null)
    {
        var props = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["handles"] = AutoCadSchemas.StringArray("Entity handles"),
        };
        if (extraProperties is not null)
        {
            foreach (var item in extraProperties)
                props[item.Key] = item.Value;
        }

        return AutoCadSchemas.Object(props, required ?? ["handles"]);
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
