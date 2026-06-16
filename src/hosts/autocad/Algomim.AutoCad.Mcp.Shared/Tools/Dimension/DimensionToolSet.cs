using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Algomim.Aec.Mcp.Tooling;
using Algomim.AutoCad.Mcp.Harness;
using Algomim.AutoCad.Mcp.Tools.Common;

namespace Algomim.AutoCad.Mcp.Tools.Dimension;

internal static class DimensionToolSet
{
    public static IEnumerable<IMcpTool> Create(AutoCadToolServices services)
    {
        yield return Linear(services);
        yield return Aligned(services);
        yield return Radial(services);
        yield return Angular(services);
        yield return AutoRoom(services);
    }

    private static IMcpTool Linear(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "dimension_add_linear",
            "Adds a horizontal, vertical, or rotated linear dimension.",
            DimensionSchema(("rotation", "number", "Rotation angle in degrees")),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var layer = reader.GetString("layer");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    AutoCadDb.EnsureLayer(database, transaction, layer);
                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForWrite);
                    var dim = new RotatedDimension(
                        DegreesToRadians(reader.GetDouble("rotation")),
                        AutoCadDb.Point(reader.RequireDouble("x1"), reader.RequireDouble("y1"), reader.GetDouble("z1")),
                        AutoCadDb.Point(reader.RequireDouble("x2"), reader.RequireDouble("y2"), reader.GetDouble("z2")),
                        AutoCadDb.Point(reader.RequireDouble("dimX"), reader.RequireDouble("dimY"), reader.GetDouble("dimZ")),
                        reader.GetString("text", string.Empty) ?? string.Empty,
                        database.Dimstyle);
                    AutoCadDb.SetLayer(dim, layer);
                    AutoCadDb.AddEntity(model, transaction, dim);
                    return AutoCadDb.EntitySummary(dim);
                });
                return ToolResponse.Success(data, "Added linear dimension.");
            }));

    private static IMcpTool Aligned(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "dimension_add_aligned",
            "Adds an aligned dimension.",
            DimensionSchema(),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var layer = reader.GetString("layer");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    AutoCadDb.EnsureLayer(database, transaction, layer);
                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForWrite);
                    var dim = new AlignedDimension(
                        AutoCadDb.Point(reader.RequireDouble("x1"), reader.RequireDouble("y1"), reader.GetDouble("z1")),
                        AutoCadDb.Point(reader.RequireDouble("x2"), reader.RequireDouble("y2"), reader.GetDouble("z2")),
                        AutoCadDb.Point(reader.RequireDouble("dimX"), reader.RequireDouble("dimY"), reader.GetDouble("dimZ")),
                        reader.GetString("text", string.Empty) ?? string.Empty,
                        database.Dimstyle);
                    AutoCadDb.SetLayer(dim, layer);
                    AutoCadDb.AddEntity(model, transaction, dim);
                    return AutoCadDb.EntitySummary(dim);
                });
                return ToolResponse.Success(data, "Added aligned dimension.");
            }));

    private static IMcpTool Radial(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "dimension_add_radial",
            "Adds a radial dimension for a circle or arc.",
            AutoCadSchemas.Object(
                AutoCadSchemas.Props(
                    ("handle", "string", "Circle or arc handle"),
                    ("leaderLength", "number", "Leader length"),
                    ("layer", "string", "Target layer")),
                ["handle"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var layer = reader.GetString("layer");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    AutoCadDb.EnsureLayer(database, transaction, layer);
                    var entity = transaction.GetObject(AutoCadDb.ObjectIdFromHandle(database, reader.RequireString("handle")), OpenMode.ForRead);
                    Point3d center;
                    Point3d chord;
                    switch (entity)
                    {
                        case Circle circle:
                            center = circle.Center;
                            chord = center + new Vector3d(circle.Radius, 0, 0);
                            break;
                        case Arc arc:
                            center = arc.Center;
                            chord = arc.StartPoint;
                            break;
                        default:
                            throw new ArgumentException("Entity must be a circle or arc.");
                    }

                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForWrite);
                    var dim = new RadialDimension(center, chord, reader.GetDouble("leaderLength", 1), string.Empty, database.Dimstyle);
                    AutoCadDb.SetLayer(dim, layer);
                    AutoCadDb.AddEntity(model, transaction, dim);
                    return AutoCadDb.EntitySummary(dim);
                });
                return ToolResponse.Success(data, "Added radial dimension.");
            }));

    private static IMcpTool Angular(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "dimension_add_angular",
            "Adds a point-three angular dimension.",
            AutoCadSchemas.Object(
                AutoCadSchemas.Props(
                    ("centerX", "number", "Angle center X"),
                    ("centerY", "number", "Angle center Y"),
                    ("x1", "number", "First ray point X"),
                    ("y1", "number", "First ray point Y"),
                    ("x2", "number", "Second ray point X"),
                    ("y2", "number", "Second ray point Y"),
                    ("arcX", "number", "Dimension arc point X"),
                    ("arcY", "number", "Dimension arc point Y"),
                    ("layer", "string", "Target layer")),
                ["centerX", "centerY", "x1", "y1", "x2", "y2", "arcX", "arcY"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var layer = reader.GetString("layer");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    AutoCadDb.EnsureLayer(database, transaction, layer);
                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForWrite);
                    var dim = new Point3AngularDimension(
                        AutoCadDb.Point(reader.RequireDouble("centerX"), reader.RequireDouble("centerY")),
                        AutoCadDb.Point(reader.RequireDouble("x1"), reader.RequireDouble("y1")),
                        AutoCadDb.Point(reader.RequireDouble("x2"), reader.RequireDouble("y2")),
                        AutoCadDb.Point(reader.RequireDouble("arcX"), reader.RequireDouble("arcY")),
                        string.Empty,
                        database.Dimstyle);
                    AutoCadDb.SetLayer(dim, layer);
                    AutoCadDb.AddEntity(model, transaction, dim);
                    return AutoCadDb.EntitySummary(dim);
                });
                return ToolResponse.Success(data, "Added angular dimension.");
            }));

    private static IMcpTool AutoRoom(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "dimension_auto_room",
            "Adds four aligned dimensions around a rectangular room bounding box.",
            AutoCadSchemas.Object(
                AutoCadSchemas.Props(
                    ("x1", "number", "Room minimum X"),
                    ("y1", "number", "Room minimum Y"),
                    ("x2", "number", "Room maximum X"),
                    ("y2", "number", "Room maximum Y"),
                    ("offset", "number", "Dimension offset"),
                    ("layer", "string", "Target layer")),
                ["x1", "y1", "x2", "y2"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var layer = reader.GetString("layer");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    AutoCadDb.EnsureLayer(database, transaction, layer);
                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForWrite);
                    var x1 = reader.RequireDouble("x1");
                    var y1 = reader.RequireDouble("y1");
                    var x2 = reader.RequireDouble("x2");
                    var y2 = reader.RequireDouble("y2");
                    var offset = reader.GetDouble("offset", 1);

                    var dims = new[]
                    {
                        new AlignedDimension(AutoCadDb.Point(x1, y1), AutoCadDb.Point(x2, y1), AutoCadDb.Point((x1 + x2) / 2, y1 - offset), string.Empty, database.Dimstyle),
                        new AlignedDimension(AutoCadDb.Point(x1, y2), AutoCadDb.Point(x2, y2), AutoCadDb.Point((x1 + x2) / 2, y2 + offset), string.Empty, database.Dimstyle),
                        new AlignedDimension(AutoCadDb.Point(x1, y1), AutoCadDb.Point(x1, y2), AutoCadDb.Point(x1 - offset, (y1 + y2) / 2), string.Empty, database.Dimstyle),
                        new AlignedDimension(AutoCadDb.Point(x2, y1), AutoCadDb.Point(x2, y2), AutoCadDb.Point(x2 + offset, (y1 + y2) / 2), string.Empty, database.Dimstyle),
                    };

                    var created = new List<object>();
                    foreach (var dim in dims)
                    {
                        AutoCadDb.SetLayer(dim, layer);
                        AutoCadDb.AddEntity(model, transaction, dim);
                        created.Add(AutoCadDb.EntitySummary(dim));
                    }

                    return new { count = created.Count, dimensions = created };
                });
                return ToolResponse.Success(data, $"Added {data.count} room dimensions.");
            }));

    private static System.Text.Json.JsonElement DimensionSchema(params (string Name, string Type, string Description)[] extra)
    {
        var props = AutoCadSchemas.Props(
            ("x1", "number", "First point X"),
            ("y1", "number", "First point Y"),
            ("z1", "number", "First point Z"),
            ("x2", "number", "Second point X"),
            ("y2", "number", "Second point Y"),
            ("z2", "number", "Second point Z"),
            ("dimX", "number", "Dimension line X"),
            ("dimY", "number", "Dimension line Y"),
            ("dimZ", "number", "Dimension line Z"),
            ("text", "string", "Dimension text override"),
            ("layer", "string", "Target layer"));

        foreach (var item in extra)
            props[item.Name] = new Dictionary<string, object> { ["type"] = item.Type, ["description"] = item.Description };

        return AutoCadSchemas.Object(props, ["x1", "y1", "x2", "y2", "dimX", "dimY"]);
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
