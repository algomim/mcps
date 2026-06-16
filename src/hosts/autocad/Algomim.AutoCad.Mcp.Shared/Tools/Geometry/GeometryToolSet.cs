using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Algomim.Aec.Mcp.Tooling;
using Algomim.AutoCad.Mcp.Harness;
using Algomim.AutoCad.Mcp.Tools.Common;

namespace Algomim.AutoCad.Mcp.Tools.Geometry;

internal static class GeometryToolSet
{
    public static IEnumerable<IMcpTool> Create(AutoCadToolServices services)
    {
        yield return CreateLine(services);
        yield return CreateCircle(services);
        yield return CreateArc(services);
        yield return CreatePolyline(services);
        yield return CreateRectangle(services);
        yield return CreateEllipse(services);
        yield return CreateSpline(services);
        yield return CreateHatch(services);
        yield return CreateRegion(services);
    }

    private static IMcpTool CreateLine(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "geometry_create_line",
            "Creates a line from start and end coordinates.",
            AutoCadSchemas.Object(
                AutoCadSchemas.Props(
                    ("startX", "number", "Start X coordinate"),
                    ("startY", "number", "Start Y coordinate"),
                    ("startZ", "number", "Start Z coordinate"),
                    ("endX", "number", "End X coordinate"),
                    ("endY", "number", "End Y coordinate"),
                    ("endZ", "number", "End Z coordinate"),
                    ("layer", "string", "Target layer name")),
                ["startX", "startY", "endX", "endY"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var layer = reader.GetString("layer");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    AutoCadDb.EnsureLayer(database, transaction, layer);
                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForWrite);
                    var line = new Line(
                        AutoCadDb.Point(reader.RequireDouble("startX"), reader.RequireDouble("startY"), reader.GetDouble("startZ")),
                        AutoCadDb.Point(reader.RequireDouble("endX"), reader.RequireDouble("endY"), reader.GetDouble("endZ")));
                    AutoCadDb.SetLayer(line, layer);
                    AutoCadDb.AddEntity(model, transaction, line);
                    return AutoCadDb.EntitySummary(line);
                });
                return ToolResponse.Success(data, "Created line.");
            }));

    private static IMcpTool CreateCircle(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "geometry_create_circle",
            "Creates a circle by center and radius.",
            AutoCadSchemas.Object(
                AutoCadSchemas.Props(
                    ("centerX", "number", "Center X coordinate"),
                    ("centerY", "number", "Center Y coordinate"),
                    ("centerZ", "number", "Center Z coordinate"),
                    ("radius", "number", "Circle radius"),
                    ("layer", "string", "Target layer name")),
                ["centerX", "centerY", "radius"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var layer = reader.GetString("layer");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    AutoCadDb.EnsureLayer(database, transaction, layer);
                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForWrite);
                    var circle = new Circle(
                        AutoCadDb.Point(reader.RequireDouble("centerX"), reader.RequireDouble("centerY"), reader.GetDouble("centerZ")),
                        Vector3d.ZAxis,
                        reader.RequireDouble("radius"));
                    AutoCadDb.SetLayer(circle, layer);
                    AutoCadDb.AddEntity(model, transaction, circle);
                    return AutoCadDb.EntitySummary(circle);
                });
                return ToolResponse.Success(data, "Created circle.");
            }));

    private static IMcpTool CreateArc(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "geometry_create_arc",
            "Creates an arc by center, radius, and start/end angles in degrees.",
            AutoCadSchemas.Object(
                AutoCadSchemas.Props(
                    ("centerX", "number", "Center X coordinate"),
                    ("centerY", "number", "Center Y coordinate"),
                    ("centerZ", "number", "Center Z coordinate"),
                    ("radius", "number", "Arc radius"),
                    ("startAngle", "number", "Start angle in degrees"),
                    ("endAngle", "number", "End angle in degrees"),
                    ("layer", "string", "Target layer name")),
                ["centerX", "centerY", "radius", "startAngle", "endAngle"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var layer = reader.GetString("layer");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    AutoCadDb.EnsureLayer(database, transaction, layer);
                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForWrite);
                    var arc = new Arc(
                        AutoCadDb.Point(reader.RequireDouble("centerX"), reader.RequireDouble("centerY"), reader.GetDouble("centerZ")),
                        reader.RequireDouble("radius"),
                        DegreesToRadians(reader.RequireDouble("startAngle")),
                        DegreesToRadians(reader.RequireDouble("endAngle")));
                    AutoCadDb.SetLayer(arc, layer);
                    AutoCadDb.AddEntity(model, transaction, arc);
                    return AutoCadDb.EntitySummary(arc);
                });
                return ToolResponse.Success(data, "Created arc.");
            }));

    private static IMcpTool CreatePolyline(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "geometry_create_polyline",
            "Creates a 2D polyline from points with optional bulge values.",
            AutoCadSchemas.Object(
                new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["points"] = AutoCadSchemas.PointArray("Polyline vertices", withBulge: true),
                    ["closed"] = new Dictionary<string, object> { ["type"] = "boolean", ["description"] = "Whether the polyline is closed" },
                    ["layer"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Target layer name" },
                },
                ["points"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var points = reader.RequirePointArray("points");
                if (points.Count < 2)
                    throw new ArgumentException("At least two points are required.");

                var layer = reader.GetString("layer");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    AutoCadDb.EnsureLayer(database, transaction, layer);
                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForWrite);
                    var polyline = new Polyline();
                    for (var index = 0; index < points.Count; index++)
                        polyline.AddVertexAt(index, AutoCadDb.Point2(points[index].X, points[index].Y), points[index].Bulge, 0, 0);
                    polyline.Closed = reader.GetBool("closed") ?? false;
                    AutoCadDb.SetLayer(polyline, layer);
                    AutoCadDb.AddEntity(model, transaction, polyline);
                    return AutoCadDb.EntitySummary(polyline);
                });
                return ToolResponse.Success(data, "Created polyline.");
            }));

    private static IMcpTool CreateRectangle(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "geometry_create_rectangle",
            "Creates a closed rectangular polyline from two corner points.",
            AutoCadSchemas.Object(
                AutoCadSchemas.Props(
                    ("x1", "number", "First corner X"),
                    ("y1", "number", "First corner Y"),
                    ("x2", "number", "Opposite corner X"),
                    ("y2", "number", "Opposite corner Y"),
                    ("layer", "string", "Target layer name")),
                ["x1", "y1", "x2", "y2"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var x1 = reader.RequireDouble("x1");
                var y1 = reader.RequireDouble("y1");
                var x2 = reader.RequireDouble("x2");
                var y2 = reader.RequireDouble("y2");
                var layer = reader.GetString("layer");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    AutoCadDb.EnsureLayer(database, transaction, layer);
                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForWrite);
                    var rectangle = new Polyline();
                    rectangle.AddVertexAt(0, AutoCadDb.Point2(x1, y1), 0, 0, 0);
                    rectangle.AddVertexAt(1, AutoCadDb.Point2(x2, y1), 0, 0, 0);
                    rectangle.AddVertexAt(2, AutoCadDb.Point2(x2, y2), 0, 0, 0);
                    rectangle.AddVertexAt(3, AutoCadDb.Point2(x1, y2), 0, 0, 0);
                    rectangle.Closed = true;
                    AutoCadDb.SetLayer(rectangle, layer);
                    AutoCadDb.AddEntity(model, transaction, rectangle);
                    return new { entity = AutoCadDb.EntitySummary(rectangle), area = Math.Abs((x2 - x1) * (y2 - y1)) };
                });
                return ToolResponse.Success(data, "Created rectangle.");
            }));

    private static IMcpTool CreateEllipse(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "geometry_create_ellipse",
            "Creates an ellipse by center, major/minor radii, and rotation.",
            AutoCadSchemas.Object(
                AutoCadSchemas.Props(
                    ("centerX", "number", "Center X coordinate"),
                    ("centerY", "number", "Center Y coordinate"),
                    ("majorRadius", "number", "Major radius"),
                    ("minorRadius", "number", "Minor radius"),
                    ("rotation", "number", "Major axis rotation in degrees"),
                    ("layer", "string", "Target layer name")),
                ["centerX", "centerY", "majorRadius", "minorRadius"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var layer = reader.GetString("layer");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    AutoCadDb.EnsureLayer(database, transaction, layer);
                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForWrite);
                    var major = reader.RequireDouble("majorRadius");
                    var minor = reader.RequireDouble("minorRadius");
                    var rotation = DegreesToRadians(reader.GetDouble("rotation"));
                    var majorAxis = new Vector3d(Math.Cos(rotation) * major, Math.Sin(rotation) * major, 0);
                    var ellipse = new Ellipse(
                        AutoCadDb.Point(reader.RequireDouble("centerX"), reader.RequireDouble("centerY")),
                        Vector3d.ZAxis,
                        majorAxis,
                        minor / major,
                        0,
                        Math.PI * 2);
                    AutoCadDb.SetLayer(ellipse, layer);
                    AutoCadDb.AddEntity(model, transaction, ellipse);
                    return AutoCadDb.EntitySummary(ellipse);
                });
                return ToolResponse.Success(data, "Created ellipse.");
            }));

    private static IMcpTool CreateSpline(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "geometry_create_spline",
            "Creates a spline through fit points.",
            AutoCadSchemas.Object(
                new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["points"] = AutoCadSchemas.PointArray("Spline fit points"),
                    ["closed"] = new Dictionary<string, object> { ["type"] = "boolean", ["description"] = "Append first point to close the spline" },
                    ["layer"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Target layer name" },
                },
                ["points"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var points = reader.RequirePointArray("points").ToList();
                if (points.Count < 2)
                    throw new ArgumentException("At least two points are required.");
                if (reader.GetBool("closed") == true)
                    points.Add(points[0]);

                var layer = reader.GetString("layer");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    AutoCadDb.EnsureLayer(database, transaction, layer);
                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForWrite);
                    var fitPoints = new Point3dCollection(points.Select(point => AutoCadDb.Point(point.X, point.Y, point.Z)).ToArray());
                    var spline = new Spline(fitPoints, 3, 0.0);
                    AutoCadDb.SetLayer(spline, layer);
                    AutoCadDb.AddEntity(model, transaction, spline);
                    return AutoCadDb.EntitySummary(spline);
                });
                return ToolResponse.Success(data, "Created spline.");
            }));

    private static IMcpTool CreateHatch(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "geometry_create_hatch",
            "Creates a hatch inside boundary curve handles.",
            AutoCadSchemas.Object(
                new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["boundaryHandles"] = AutoCadSchemas.StringArray("Closed boundary curve handles"),
                    ["pattern"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Hatch pattern, for example SOLID or ANSI31" },
                    ["scale"] = new Dictionary<string, object> { ["type"] = "number", ["description"] = "Pattern scale" },
                    ["angle"] = new Dictionary<string, object> { ["type"] = "number", ["description"] = "Pattern angle in degrees" },
                    ["layer"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Target layer name" },
                },
                ["boundaryHandles"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var handles = reader.RequireStringArray("boundaryHandles");
                var layer = reader.GetString("layer");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    AutoCadDb.EnsureLayer(database, transaction, layer);
                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForWrite);
                    var ids = new ObjectIdCollection(handles.Select(handle => AutoCadDb.ObjectIdFromHandle(database, handle)).ToArray());
                    var hatch = new Hatch();
                    hatch.SetDatabaseDefaults();
                    AutoCadDb.SetLayer(hatch, layer);
                    AutoCadDb.AddEntity(model, transaction, hatch);
                    hatch.Associative = true;
                    hatch.PatternScale = reader.GetDouble("scale", 1);
                    hatch.PatternAngle = DegreesToRadians(reader.GetDouble("angle"));
                    hatch.SetHatchPattern(HatchPatternType.PreDefined, reader.GetString("pattern", "SOLID") ?? "SOLID");
                    hatch.AppendLoop(HatchLoopTypes.Outermost, ids);
                    hatch.EvaluateHatch(true);
                    return AutoCadDb.EntitySummary(hatch);
                });
                return ToolResponse.Success(data, "Created hatch.");
            }));

    private static IMcpTool CreateRegion(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "geometry_create_region",
            "Creates region entities from closed curve handles.",
            AutoCadSchemas.Object(
                new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["handles"] = AutoCadSchemas.StringArray("Closed curve handles"),
                    ["layer"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Target layer name" },
                },
                ["handles"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var handles = reader.RequireStringArray("handles");
                var layer = reader.GetString("layer");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    AutoCadDb.EnsureLayer(database, transaction, layer);
                    var curves = new DBObjectCollection();
                    foreach (var handle in handles)
                    {
                        var curve = transaction.GetObject(AutoCadDb.ObjectIdFromHandle(database, handle), OpenMode.ForRead) as Curve;
                        if (curve is not null)
                            curves.Add(curve);
                    }

                    if (curves.Count == 0)
                        throw new ArgumentException("No valid curve handles were supplied.");

                    var regions = Region.CreateFromCurves(curves);
                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForWrite);
                    var created = new List<object>();
                    foreach (Region region in regions)
                    {
                        AutoCadDb.SetLayer(region, layer);
                        AutoCadDb.AddEntity(model, transaction, region);
                        created.Add(AutoCadDb.EntitySummary(region));
                    }

                    return new { count = created.Count, regions = created };
                });
                return ToolResponse.Success(data, $"Created {data.count} region(s).");
            }));

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
