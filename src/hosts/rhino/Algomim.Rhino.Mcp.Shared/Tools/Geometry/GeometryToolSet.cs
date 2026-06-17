using Algomim.Aec.Mcp.Tooling;
using Algomim.Rhino.Mcp.Tools.Common;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace Algomim.Rhino.Mcp.Tools.Geometry;

internal static class GeometryToolSet
{
    public static IEnumerable<IMcpTool> Create()
    {
        yield return CreatePoint();
        yield return CreateLine();
        yield return CreatePolyline();
        yield return CreateCircle();
        yield return CreateArc();
        yield return CreateEllipse();
        yield return CreateBox();
        yield return CreateSphere();
        yield return CreateCylinder();
        yield return CreateCone();
        yield return ExtrudeCurve();
        yield return CreateLoft();
        yield return CreatePipe();
        yield return OffsetCurve();
        yield return ProjectCurve();
        yield return IntersectCurves();
        yield return SplitCurve();
        yield return CreateSweep1();
        yield return BooleanUnion();
        yield return BooleanDifference();
        yield return BooleanIntersection();
    }

    private static IMcpTool CreatePoint()
        => new DelegateRhinoTool(
            "geometry_create_point",
            "Creates a Rhino point object.",
            RhinoSchemas.Object(CommonProperties(("point", RhinoSchemas.Point("Point position"))), ["point"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var id = document.Objects.AddPoint(
                    reader.RequirePoint("point"),
                    RhinoAttributes.FromArguments(document, reader));

                return Created(document, id, "Created point.");
            }));

    private static IMcpTool CreateLine()
        => new DelegateRhinoTool(
            "geometry_create_line",
            "Creates a Rhino line curve from start and end points.",
            RhinoSchemas.Object(CommonProperties(
                ("start", RhinoSchemas.Point("Line start point")),
                ("end", RhinoSchemas.Point("Line end point"))), ["start", "end"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var id = document.Objects.AddLine(
                    reader.RequirePoint("start"),
                    reader.RequirePoint("end"),
                    RhinoAttributes.FromArguments(document, reader));

                return Created(document, id, "Created line.");
            }));

    private static IMcpTool CreatePolyline()
        => new DelegateRhinoTool(
            "geometry_create_polyline",
            "Creates a Rhino polyline curve from two or more points.",
            RhinoSchemas.Object(CommonProperties(("points", RhinoSchemas.PointArray("Polyline points"))), ["points"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var points = reader.RequirePointArray("points");
                if (points.Count < 2)
                    throw new ArgumentException("Argument 'points' must contain at least two points.");

                var id = document.Objects.AddPolyline(points, RhinoAttributes.FromArguments(document, reader));
                return Created(document, id, "Created polyline.");
            }));

    private static IMcpTool CreateCircle()
        => new DelegateRhinoTool(
            "geometry_create_circle",
            "Creates a Rhino circle curve by center and radius.",
            RhinoSchemas.Object(CommonProperties(
                ("center", RhinoSchemas.Point("Circle center point")),
                ("radius", RhinoSchemas.Number("Circle radius", minimum: 0))), ["center", "radius"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var radius = RequirePositive(reader, "radius");
                var id = document.Objects.AddCircle(
                    new Circle(reader.RequirePoint("center"), radius),
                    RhinoAttributes.FromArguments(document, reader));

                return Created(document, id, "Created circle.");
            }));

    private static IMcpTool CreateArc()
        => new DelegateRhinoTool(
            "geometry_create_arc",
            "Creates a Rhino arc curve on the world XY plane by center, radius, and angle in degrees.",
            RhinoSchemas.Object(CommonProperties(
                ("center", RhinoSchemas.Point("Arc center point")),
                ("radius", RhinoSchemas.Number("Arc radius", minimum: 0)),
                ("angle", RhinoSchemas.Number("Arc angle in degrees"))), ["center", "radius", "angle"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var radius = RequirePositive(reader, "radius");
                var arc = new Arc(new Plane(reader.RequirePoint("center"), Vector3d.ZAxis), radius, DegreesToRadians(reader.RequireDouble("angle")));
                var id = document.Objects.AddArc(arc, RhinoAttributes.FromArguments(document, reader));
                return Created(document, id, "Created arc.");
            }));

    private static IMcpTool CreateEllipse()
        => new DelegateRhinoTool(
            "geometry_create_ellipse",
            "Creates a Rhino ellipse curve by center and X/Y radii.",
            RhinoSchemas.Object(CommonProperties(
                ("center", RhinoSchemas.Point("Ellipse center point")),
                ("radiusX", RhinoSchemas.Number("Ellipse X radius", minimum: 0)),
                ("radiusY", RhinoSchemas.Number("Ellipse Y radius", minimum: 0))), ["center", "radiusX", "radiusY"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var ellipse = new Ellipse(
                    new Plane(reader.RequirePoint("center"), Vector3d.ZAxis),
                    RequirePositive(reader, "radiusX"),
                    RequirePositive(reader, "radiusY"));
                var id = document.Objects.AddEllipse(ellipse, RhinoAttributes.FromArguments(document, reader));
                return Created(document, id, "Created ellipse.");
            }));

    private static IMcpTool CreateBox()
        => new DelegateRhinoTool(
            "geometry_create_box",
            "Creates a Rhino box centered at a point.",
            RhinoSchemas.Object(CommonProperties(
                ("center", RhinoSchemas.Point("Box center point; defaults to origin")),
                ("width", RhinoSchemas.Number("Box X size", minimum: 0)),
                ("length", RhinoSchemas.Number("Box Y size", minimum: 0)),
                ("height", RhinoSchemas.Number("Box Z size", minimum: 0))), ["width", "length", "height"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var center = reader.GetPoint("center", Point3d.Origin);
                var width = RequirePositive(reader, "width");
                var length = RequirePositive(reader, "length");
                var height = RequirePositive(reader, "height");
                var plane = Plane.WorldXY;
                plane.Origin = center;
                var box = new Box(
                    plane,
                    new Interval(-width / 2, width / 2),
                    new Interval(-length / 2, length / 2),
                    new Interval(-height / 2, height / 2));
                var id = document.Objects.AddBox(box, RhinoAttributes.FromArguments(document, reader));
                return Created(document, id, "Created box.");
            }));

    private static IMcpTool CreateSphere()
        => new DelegateRhinoTool(
            "geometry_create_sphere",
            "Creates a Rhino sphere by center and radius.",
            RhinoSchemas.Object(CommonProperties(
                ("center", RhinoSchemas.Point("Sphere center point; defaults to origin")),
                ("radius", RhinoSchemas.Number("Sphere radius", minimum: 0))), ["radius"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var sphere = new Sphere(reader.GetPoint("center", Point3d.Origin), RequirePositive(reader, "radius"));
                var id = document.Objects.AddBrep(sphere.ToBrep(), RhinoAttributes.FromArguments(document, reader));
                return Created(document, id, "Created sphere.");
            }));

    private static IMcpTool CreateCylinder()
        => new DelegateRhinoTool(
            "geometry_create_cylinder",
            "Creates a Rhino cylinder from a base center, radius, and height.",
            RhinoSchemas.Object(CommonProperties(
                ("baseCenter", RhinoSchemas.Point("Cylinder base center point; defaults to origin")),
                ("radius", RhinoSchemas.Number("Cylinder radius", minimum: 0)),
                ("height", RhinoSchemas.Number("Cylinder height", minimum: 0)),
                ("cap", RhinoSchemas.Boolean("Cap cylinder ends", defaultValue: true))), ["radius", "height"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var circle = new Circle(new Plane(reader.GetPoint("baseCenter", Point3d.Origin), Vector3d.ZAxis), RequirePositive(reader, "radius"));
                var cylinder = new Cylinder(circle, RequirePositive(reader, "height"));
                var cap = reader.GetBool("cap", fallback: true);
                var id = document.Objects.AddBrep(cylinder.ToBrep(cap, cap), RhinoAttributes.FromArguments(document, reader));
                return Created(document, id, "Created cylinder.");
            }));

    private static IMcpTool CreateCone()
        => new DelegateRhinoTool(
            "geometry_create_cone",
            "Creates a Rhino cone from a base center, base radius, and height.",
            RhinoSchemas.Object(CommonProperties(
                ("baseCenter", RhinoSchemas.Point("Cone base center point; defaults to origin")),
                ("radius", RhinoSchemas.Number("Cone base radius", minimum: 0)),
                ("height", RhinoSchemas.Number("Cone height", minimum: 0)),
                ("cap", RhinoSchemas.Boolean("Cap cone base", defaultValue: true))), ["radius", "height"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var cone = new Cone(new Plane(reader.GetPoint("baseCenter", Point3d.Origin), Vector3d.ZAxis), RequirePositive(reader, "height"), RequirePositive(reader, "radius"));
                var brep = Brep.CreateFromCone(cone, reader.GetBool("cap", fallback: true))
                    ?? throw new InvalidOperationException("Cone creation failed.");
                var id = document.Objects.AddBrep(brep, RhinoAttributes.FromArguments(document, reader));
                return Created(document, id, "Created cone.");
            }));

    private static IMcpTool ExtrudeCurve()
        => new DelegateRhinoTool(
            "geometry_extrude_curve",
            "Extrudes a curve along a direction vector.",
            RhinoSchemas.Object(CommonProperties(
                ("curveId", new Dictionary<string, object> { ["type"] = "string", ["description"] = "Curve object GUID" }),
                ("direction", RhinoSchemas.Point("Extrusion direction vector")),
                ("cap", RhinoSchemas.Boolean("Cap closed planar curves", defaultValue: true))), ["curveId", "direction"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var curve = RhinoIds.RequireCurve(document, reader.RequireGuid("curveId"));
                var direction = reader.RequireVector("direction");
                if (!direction.IsValid || direction.IsTiny(document.ModelAbsoluteTolerance))
                    throw new ArgumentException("Argument 'direction' must be a non-zero vector.");

                var surface = Surface.CreateExtrusion(curve, direction)
                    ?? throw new InvalidOperationException("Extrusion failed.");
                var attributes = RhinoAttributes.FromArguments(document, reader);
                Guid id;
                if (reader.GetBool("cap", fallback: true) && curve.IsClosed)
                {
                    var capped = surface.ToBrep()?.CapPlanarHoles(document.ModelAbsoluteTolerance);
                    id = capped is null
                        ? document.Objects.AddSurface(surface, attributes)
                        : document.Objects.AddBrep(capped, attributes);
                }
                else
                {
                    id = document.Objects.AddSurface(surface, attributes);
                }

                return Created(document, id, "Created extrusion.");
            }));

    private static IMcpTool CreateLoft()
        => new DelegateRhinoTool(
            "geometry_create_loft",
            "Creates a loft through two or more curve objects.",
            RhinoSchemas.Object(CommonProperties(
                ("curveIds", RhinoSchemas.StringArray("Curve object GUIDs")),
                ("closed", RhinoSchemas.Boolean("Close loft across the first and last curves")),
                ("loftType", new Dictionary<string, object> { ["type"] = "string", ["description"] = "normal, loose, tight, or straight", ["default"] = "normal" })), ["curveIds"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var curves = reader.RequireGuidArray("curveIds").Select(id => RhinoIds.RequireCurve(document, id)).ToArray();
                if (curves.Length < 2)
                    throw new ArgumentException("Argument 'curveIds' must contain at least two curve IDs.");

                var loftType = ParseLoftType(reader.GetString("loftType", "normal"));
                var breps = Brep.CreateFromLoft(curves, Point3d.Unset, Point3d.Unset, loftType, reader.GetBool("closed"));
                return CreatedMany(document, breps, RhinoAttributes.FromArguments(document, reader), "Created loft.");
            }));

    private static IMcpTool CreatePipe()
        => new DelegateRhinoTool(
            "geometry_create_pipe",
            "Creates a pipe Brep along a curve.",
            RhinoSchemas.Object(CommonProperties(
                ("curveId", new Dictionary<string, object> { ["type"] = "string", ["description"] = "Rail curve object GUID" }),
                ("radius", RhinoSchemas.Number("Pipe radius", minimum: 0)),
                ("cap", RhinoSchemas.Boolean("Cap pipe ends", defaultValue: true))), ["curveId", "radius"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var curve = RhinoIds.RequireCurve(document, reader.RequireGuid("curveId"));
                var breps = Brep.CreatePipe(
                    curve,
                    RequirePositive(reader, "radius"),
                    localBlending: true,
                    cap: reader.GetBool("cap", fallback: true) ? PipeCapMode.Flat : PipeCapMode.None,
                    fitRail: false,
                    absoluteTolerance: document.ModelAbsoluteTolerance,
                    angleToleranceRadians: document.ModelAngleToleranceRadians);

                return CreatedMany(document, breps, RhinoAttributes.FromArguments(document, reader), "Created pipe.");
            }));

    private static IMcpTool OffsetCurve()
        => new DelegateRhinoTool(
            "geometry_offset_curve",
            "Offsets a planar curve.",
            RhinoSchemas.Object(CommonProperties(
                ("curveId", new Dictionary<string, object> { ["type"] = "string", ["description"] = "Curve object GUID" }),
                ("distance", RhinoSchemas.Number("Offset distance"))), ["curveId", "distance"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var curve = RhinoIds.RequireCurve(document, reader.RequireGuid("curveId"));
                var distance = reader.RequireDouble("distance");
                if (Math.Abs(distance) < document.ModelAbsoluteTolerance)
                    throw new ArgumentException("Argument 'distance' is too small.");

                if (!curve.TryGetPlane(out var plane))
                {
                    plane = Plane.WorldXY;
                    plane.Origin = curve.PointAtStart;
                }

                var curves = curve.Offset(plane, distance, document.ModelAbsoluteTolerance, CurveOffsetCornerStyle.Sharp);
                return CreatedMany(document, curves, RhinoAttributes.FromArguments(document, reader), "Created offset curve.");
            }));

    private static IMcpTool ProjectCurve()
        => new DelegateRhinoTool(
            "geometry_project_curve",
            "Projects a curve onto Brep, extrusion, or mesh targets along a direction vector.",
            RhinoSchemas.Object(CommonProperties(
                ("curveId", new Dictionary<string, object> { ["type"] = "string", ["description"] = "Curve object GUID" }),
                ("targetIds", RhinoSchemas.StringArray("Target Brep, extrusion, or mesh object GUIDs")),
                ("direction", RhinoSchemas.Point("Projection direction vector"))), ["curveId", "targetIds", "direction"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var curve = RhinoIds.RequireCurve(document, reader.RequireGuid("curveId"));
                var direction = reader.RequireVector("direction");
                if (!direction.IsValid || direction.IsTiny(document.ModelAbsoluteTolerance))
                    throw new ArgumentException("Argument 'direction' must be a non-zero vector.");

                var breps = new List<Brep>();
                var meshes = new List<Mesh>();
                foreach (var id in reader.RequireGuidArray("targetIds"))
                {
                    var obj = RhinoIds.RequireObject(document, id);
                    switch (obj.Geometry)
                    {
                        case Brep brep:
                            breps.Add(brep);
                            break;
                        case Extrusion extrusion:
                            breps.Add(extrusion.ToBrep());
                            break;
                        case Mesh mesh:
                            meshes.Add(mesh);
                            break;
                        default:
                            throw new ArgumentException($"Target object '{id:D}' must be a Brep, extrusion, or mesh.");
                    }
                }

                var projected = new List<Curve>();
                if (breps.Count > 0)
                    projected.AddRange(Curve.ProjectToBrep(curve, breps, direction, document.ModelAbsoluteTolerance) ?? Array.Empty<Curve>());
                if (meshes.Count > 0)
                    projected.AddRange(Curve.ProjectToMesh(curve, meshes, direction, document.ModelAbsoluteTolerance) ?? Array.Empty<Curve>());

                return CreatedMany(document, projected, RhinoAttributes.FromArguments(document, reader), "Projected curve.");
            }));

    private static IMcpTool IntersectCurves()
        => new DelegateRhinoTool(
            "geometry_intersect_curves",
            "Creates point and overlap-curve objects from intersections between two curves.",
            RhinoSchemas.Object(CommonProperties(
                ("curveIdA", new Dictionary<string, object> { ["type"] = "string", ["description"] = "First curve object GUID" }),
                ("curveIdB", new Dictionary<string, object> { ["type"] = "string", ["description"] = "Second curve object GUID" }),
                ("tolerance", RhinoSchemas.Number("Intersection tolerance; defaults to document tolerance", minimum: 0))), ["curveIdA", "curveIdB"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var curveA = RhinoIds.RequireCurve(document, reader.RequireGuid("curveIdA"));
                var curveB = RhinoIds.RequireCurve(document, reader.RequireGuid("curveIdB"));
                var tolerance = reader.GetDouble("tolerance", document.ModelAbsoluteTolerance);
                if (tolerance <= 0)
                    throw new ArgumentException("Argument 'tolerance' must be positive.");

                var intersections = Intersection.CurveCurve(curveA, curveB, tolerance, tolerance);
                if (intersections is null || intersections.Count == 0)
                    return ToolResponse.Success(new { count = 0, points = Array.Empty<object>(), overlaps = Array.Empty<object>() }, "No curve intersections found.");

                var attributes = RhinoAttributes.FromArguments(document, reader);
                var points = new List<object>();
                var overlaps = new List<object>();
                foreach (var item in intersections)
                {
                    if (item.IsPoint)
                    {
                        var id = document.Objects.AddPoint(item.PointA, attributes);
                        if (id != Guid.Empty)
                        {
                            points.Add(new
                            {
                                id = id.ToString("D"),
                                point = Point(item.PointA),
                                parameterA = item.ParameterA,
                                parameterB = item.ParameterB,
                            });
                        }
                    }
                    else if (item.IsOverlap)
                    {
                        var overlap = curveA.Trim(item.OverlapA.T0, item.OverlapA.T1);
                        if (overlap is null)
                            continue;

                        var id = document.Objects.AddCurve(overlap, attributes);
                        if (id != Guid.Empty)
                            overlaps.Add(RhinoObjectSummary.From(document, RhinoIds.RequireObject(document, id)));
                    }
                }

                document.Views.Redraw();
                return ToolResponse.Success(new { count = intersections.Count, points, overlaps }, $"Found {intersections.Count} curve intersection event(s).");
            }));

    private static IMcpTool SplitCurve()
        => new DelegateRhinoTool(
            "geometry_split_curve",
            "Splits a curve by curve parameters and optional point objects.",
            RhinoSchemas.Object(CommonProperties(
                ("curveId", new Dictionary<string, object> { ["type"] = "string", ["description"] = "Curve object GUID" }),
                ("parameters", RhinoSchemas.NumberArray("Curve parameters to split at")),
                ("pointIds", RhinoSchemas.StringArray("Point object GUIDs to project onto the curve for splitting")),
                ("deleteSource", RhinoSchemas.Boolean("Delete the source curve after splitting", defaultValue: true))), ["curveId"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var curveId = reader.RequireGuid("curveId");
                var curveObj = RhinoIds.RequireObject(document, curveId);
                var curve = curveObj.Geometry as Curve
                    ?? throw new ArgumentException($"Object '{curveId:D}' must be a curve.");

                var splitParameters = reader.GetDoubleArray("parameters").ToList();
                foreach (var pointId in reader.GetStringArray("pointIds"))
                {
                    if (!Guid.TryParse(pointId, out var guid))
                        throw new ArgumentException($"Argument 'pointIds' contains an invalid GUID: {pointId}");

                    var pointObj = RhinoIds.RequireObject(document, guid);
                    var point = pointObj.Geometry switch
                    {
                        Point pointGeometry => pointGeometry.Location,
                        TextDot dot => dot.Point,
                        _ => throw new ArgumentException($"Object '{guid:D}' must be a point or text dot."),
                    };
                    if (curve.ClosestPoint(point, out var t, document.ModelAbsoluteTolerance))
                        splitParameters.Add(t);
                }

                splitParameters = splitParameters
                    .Where(t => curve.Domain.IncludesParameter(t, strict: true))
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();
                if (splitParameters.Count == 0)
                    throw new ArgumentException("At least one valid split parameter or point is required.");

                var pieces = curve.Split(splitParameters);
                var response = CreatedMany(document, pieces, RhinoAttributes.FromArguments(document, reader), "Split curve.");
                if (reader.GetBool("deleteSource", fallback: true))
                    document.Objects.Delete(curveObj, true);

                return response;
            }));

    private static IMcpTool CreateSweep1()
        => new DelegateRhinoTool(
            "geometry_create_sweep1",
            "Creates Brep results by sweeping one or more profile curves along a rail curve.",
            RhinoSchemas.Object(CommonProperties(
                ("railId", new Dictionary<string, object> { ["type"] = "string", ["description"] = "Rail curve object GUID" }),
                ("profileIds", RhinoSchemas.StringArray("Profile curve object GUIDs")),
                ("closed", RhinoSchemas.Boolean("Try to create a closed sweep", defaultValue: false))), ["railId", "profileIds"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var rail = RhinoIds.RequireCurve(document, reader.RequireGuid("railId"));
                var profiles = reader.RequireGuidArray("profileIds").Select(id => RhinoIds.RequireCurve(document, id)).ToArray();
                if (profiles.Length == 0)
                    throw new ArgumentException("Argument 'profileIds' must contain at least one curve ID.");

                var sweep = new SweepOneRail();
                sweep.SetToRoadlikeTop();
                sweep.ClosedSweep = reader.GetBool("closed");
                var breps = sweep.PerformSweep(rail, profiles);
                return CreatedMany(document, breps, RhinoAttributes.FromArguments(document, reader), "Created sweep.");
            }));

    private static IMcpTool BooleanUnion()
        => new DelegateRhinoTool(
            "geometry_boolean_union",
            "Creates boolean union result Breps from two or more solid Brep-like objects.",
            RhinoSchemas.Object(CommonProperties(
                ("ids", RhinoSchemas.StringArray("Source object GUIDs")),
                ("deleteSources", RhinoSchemas.Boolean("Delete source objects after successful boolean", defaultValue: true))), ["ids"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var ids = reader.RequireGuidArray("ids");
                if (ids.Count < 2)
                    throw new ArgumentException("Argument 'ids' must contain at least two object IDs.");

                var breps = ids.Select(id => RhinoIds.RequireBrep(document, id)).ToArray();
                var results = Brep.CreateBooleanUnion(breps, document.ModelAbsoluteTolerance);
                var response = CreatedMany(document, results, RhinoAttributes.FromArguments(document, reader), "Created boolean union.");
                DeleteSources(document, ids, reader.GetBool("deleteSources", fallback: true));
                return response;
            }));

    private static IMcpTool BooleanDifference()
        => new DelegateRhinoTool(
            "geometry_boolean_difference",
            "Subtracts one or more solid Brep-like objects from a base object.",
            RhinoSchemas.Object(CommonProperties(
                ("baseId", new Dictionary<string, object> { ["type"] = "string", ["description"] = "Base object GUID" }),
                ("subtractIds", RhinoSchemas.StringArray("Subtraction object GUIDs")),
                ("deleteSources", RhinoSchemas.Boolean("Delete source objects after successful boolean", defaultValue: true))), ["baseId", "subtractIds"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var baseId = reader.RequireGuid("baseId");
                var subtractIds = reader.RequireGuidArray("subtractIds");
                var baseBreps = new[] { RhinoIds.RequireBrep(document, baseId) };
                var subtractBreps = subtractIds.Select(id => RhinoIds.RequireBrep(document, id)).ToArray();
                var results = Brep.CreateBooleanDifference(baseBreps, subtractBreps, document.ModelAbsoluteTolerance);
                var response = CreatedMany(document, results, RhinoAttributes.FromArguments(document, reader), "Created boolean difference.");
                DeleteSources(document, [baseId, .. subtractIds], reader.GetBool("deleteSources", fallback: true));
                return response;
            }));

    private static IMcpTool BooleanIntersection()
        => new DelegateRhinoTool(
            "geometry_boolean_intersection",
            "Creates boolean intersection result Breps from two or more solid Brep-like objects.",
            RhinoSchemas.Object(CommonProperties(
                ("ids", RhinoSchemas.StringArray("Source object GUIDs")),
                ("deleteSources", RhinoSchemas.Boolean("Delete source objects after successful boolean", defaultValue: true))), ["ids"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var ids = reader.RequireGuidArray("ids");
                if (ids.Count < 2)
                    throw new ArgumentException("Argument 'ids' must contain at least two object IDs.");

                var breps = ids.Select(id => RhinoIds.RequireBrep(document, id)).ToArray();
                var results = Brep.CreateBooleanIntersection(breps[0], breps[1], document.ModelAbsoluteTolerance);
                for (var index = 2; index < breps.Length && results is { Length: > 0 }; index++)
                {
                    results = results
                        .SelectMany(result => Brep.CreateBooleanIntersection(result, breps[index], document.ModelAbsoluteTolerance) ?? Array.Empty<Brep>())
                        .ToArray();
                }

                var response = CreatedMany(document, results, RhinoAttributes.FromArguments(document, reader), "Created boolean intersection.");
                DeleteSources(document, ids, reader.GetBool("deleteSources", fallback: true));
                return response;
            }));

    private static Dictionary<string, object> CommonProperties(params (string Name, object Schema)[] properties)
    {
        var result = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["name"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Optional object name" },
            ["layer"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Optional target layer name" },
            ["color"] = RhinoSchemas.Color("Optional object color as RGB"),
        };

        foreach (var (name, schema) in properties)
            result[name] = schema;

        return result;
    }

    private static ToolResponse Created(RhinoDoc document, Guid id, string summary)
    {
        if (id == Guid.Empty)
            throw new InvalidOperationException(summary.Replace(".", " failed."));

        var obj = RhinoIds.RequireObject(document, id);
        document.Views.Redraw();
        return ToolResponse.Success(RhinoObjectSummary.From(document, obj), summary);
    }

    private static ToolResponse CreatedMany<T>(RhinoDoc document, IReadOnlyList<T>? geometry, ObjectAttributes attributes, string summary)
        where T : GeometryBase
    {
        if (geometry is null || geometry.Count == 0)
            throw new InvalidOperationException(summary.Replace(".", " failed."));

        var objects = new List<object>();
        foreach (var item in geometry)
        {
            var id = item switch
            {
                Brep brep => document.Objects.AddBrep(brep, attributes),
                Curve curve => document.Objects.AddCurve(curve, attributes),
                Surface surface => document.Objects.AddSurface(surface, attributes),
                _ => Guid.Empty,
            };

            if (id != Guid.Empty)
                objects.Add(RhinoObjectSummary.From(document, RhinoIds.RequireObject(document, id)));
        }

        document.Views.Redraw();
        return ToolResponse.Success(new { count = objects.Count, objects }, $"{summary} ({objects.Count})");
    }

    private static double RequirePositive(ArgumentReader reader, string name)
    {
        var value = reader.RequireDouble(name);
        if (value <= 0)
            throw new ArgumentException($"Argument '{name}' must be positive.");

        return value;
    }

    private static void DeleteSources(RhinoDoc document, IEnumerable<Guid> ids, bool deleteSources)
    {
        if (!deleteSources)
            return;

        foreach (var id in ids)
        {
            var obj = document.Objects.FindId(id);
            if (obj is not null)
                document.Objects.Delete(obj, true);
        }
    }

    private static LoftType ParseLoftType(string? value)
        => value?.Trim().ToLowerInvariant() switch
        {
            "loose" => LoftType.Loose,
            "tight" => LoftType.Tight,
            "straight" => LoftType.Straight,
            _ => LoftType.Normal,
        };

    private static object Point(Point3d point)
        => new { x = point.X, y = point.Y, z = point.Z };

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
