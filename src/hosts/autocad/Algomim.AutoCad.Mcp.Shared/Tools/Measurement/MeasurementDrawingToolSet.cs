using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Algomim.Aec.Mcp.Tooling;
using Algomim.AutoCad.Mcp.Harness;
using Algomim.AutoCad.Mcp.Tools.Common;

namespace Algomim.AutoCad.Mcp.Tools.Measurement;

internal static class MeasurementDrawingToolSet
{
    public static IEnumerable<IMcpTool> Create(AutoCadToolServices services)
    {
        yield return Distance();
        yield return Area(services);
        yield return Extents(services);
        yield return CountEntities(services);
    }

    private static IMcpTool Distance()
        => new DelegateAutoCadTool(
            "measurement_get_distance",
            "Measures distance and delta between two points.",
            AutoCadSchemas.Object(
                AutoCadSchemas.Props(
                    ("x1", "number", "First point X"),
                    ("y1", "number", "First point Y"),
                    ("z1", "number", "First point Z"),
                    ("x2", "number", "Second point X"),
                    ("y2", "number", "Second point Y"),
                    ("z2", "number", "Second point Z")),
                ["x1", "y1", "x2", "y2"]),
            args =>
            {
                var reader = new ArgumentReader(args);
                var p1 = AutoCadDb.Point(reader.RequireDouble("x1"), reader.RequireDouble("y1"), reader.GetDouble("z1"));
                var p2 = AutoCadDb.Point(reader.RequireDouble("x2"), reader.RequireDouble("y2"), reader.GetDouble("z2"));
                var data = new
                {
                    distance = p1.DistanceTo(p2),
                    deltaX = p2.X - p1.X,
                    deltaY = p2.Y - p1.Y,
                    deltaZ = p2.Z - p1.Z,
                };
                return Task.FromResult(ToolResponse.Success(data, "Measured distance."));
            });

    private static IMcpTool Area(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "measurement_get_area",
            "Measures area and perimeter/length for supported closed entities.",
            AutoCadSchemas.Object(AutoCadSchemas.Props(("handle", "string", "Entity handle")), ["handle"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var handle = new ArgumentReader(args).RequireString("handle");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    var entity = (Autodesk.AutoCAD.DatabaseServices.Entity)transaction.GetObject(AutoCadDb.ObjectIdFromHandle(database, handle), OpenMode.ForRead);
                    return entity switch
                    {
                        Polyline polyline => new { area = polyline.Area, perimeter = (double?)polyline.Length, type = AutoCadDb.EntityType(entity) },
                        Circle circle => new { area = Math.PI * circle.Radius * circle.Radius, perimeter = (double?)(2 * Math.PI * circle.Radius), type = AutoCadDb.EntityType(entity) },
                        Region region => new { area = region.Area, perimeter = (double?)null, type = AutoCadDb.EntityType(entity) },
                        Ellipse ellipse => new { area = Math.PI * ellipse.MajorRadius * ellipse.MinorRadius, perimeter = (double?)null, type = AutoCadDb.EntityType(entity) },
                        Hatch hatch => new { area = hatch.Area, perimeter = (double?)null, type = AutoCadDb.EntityType(entity) },
                        _ => throw new ArgumentException($"Entity type {AutoCadDb.EntityType(entity)} does not expose an area measurement."),
                    };
                });
                return ToolResponse.Success(data, "Measured area.");
            }));

    private static IMcpTool Extents(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "drawing_get_extents",
            "Gets the model space geometric extents.",
            AutoCadSchemas.Empty,
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForRead);
                    Extents3d? extents = null;
                    var count = 0;
                    foreach (ObjectId id in model)
                    {
                        if (transaction.GetObject(id, OpenMode.ForRead) is not Autodesk.AutoCAD.DatabaseServices.Entity entity)
                            continue;
                        try
                        {
                            var item = entity.GeometricExtents;
                            extents = extents.HasValue ? Combine(extents.Value, item) : item;
                            count++;
                        }
                        catch
                        {
                            // Some entities do not expose extents until regen.
                        }
                    }

                    return new
                    {
                        count,
                        min = extents.HasValue ? new { x = extents.Value.MinPoint.X, y = extents.Value.MinPoint.Y, z = extents.Value.MinPoint.Z } : null,
                        max = extents.HasValue ? new { x = extents.Value.MaxPoint.X, y = extents.Value.MaxPoint.Y, z = extents.Value.MaxPoint.Z } : null,
                    };
                });
                return ToolResponse.Success(data, $"Computed extents from {data.count} entities.");
            }));

    private static IMcpTool CountEntities(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "drawing_count_entities",
            "Counts model space entities and groups them by type and layer.",
            AutoCadSchemas.Empty,
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForRead);
                    var byType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    var byLayer = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    var count = 0;

                    foreach (ObjectId id in model)
                    {
                        if (transaction.GetObject(id, OpenMode.ForRead) is not Autodesk.AutoCAD.DatabaseServices.Entity entity)
                            continue;
                        count++;
                        Increment(byType, AutoCadDb.EntityType(entity));
                        Increment(byLayer, entity.Layer);
                    }

                    return new { count, byType, byLayer };
                });
                return ToolResponse.Success(data, $"Counted {data.count} entities.");
            }));

    private static Extents3d Combine(Extents3d a, Extents3d b)
    {
        var result = a;
        result.AddExtents(b);
        return result;
    }

    private static void Increment(Dictionary<string, int> dictionary, string key)
    {
        dictionary.TryGetValue(key, out var count);
        dictionary[key] = count + 1;
    }
}
