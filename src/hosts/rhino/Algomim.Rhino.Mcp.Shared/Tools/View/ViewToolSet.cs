using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Rhino.Mcp.Tools.Common;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace Algomim.Rhino.Mcp.Tools.View;

internal static class ViewToolSet
{
    public static IEnumerable<IMcpTool> Create()
    {
        yield return ZoomExtents();
        yield return ZoomToEntity();
        yield return ZoomToLayer();
        yield return SetCamera();
        yield return CaptureSnapshot();
    }

    private static IMcpTool ZoomExtents()
        => new DelegateRhinoTool(
            "view_zoom_extents",
            "Frames all visible Rhino document geometry in the active viewport.",
            RhinoSchemas.Empty,
            _ => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var view = document.Views.ActiveView;
                if (view is null)
                    throw new InvalidOperationException("No active Rhino viewport is available.");

                var box = SceneBoundingBox(document);
                if (!box.IsValid)
                    throw new InvalidOperationException("No visible geometry is available to frame.");

                view.ActiveViewport.ZoomBoundingBox(box);
                view.Redraw();
                return ToolResponse.Success(new { boundingBox = Box(box) }, "Zoomed active viewport to extents.");
            }));

    private static IMcpTool ZoomToEntity()
        => new DelegateRhinoTool(
            "view_zoom_to_entity",
            "Frames one or more Rhino objects in the active viewport.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["ids"] = RhinoSchemas.StringArray("Object GUIDs"),
            }, ["ids"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var view = document.Views.ActiveView ?? throw new InvalidOperationException("No active Rhino viewport is available.");
                var box = BoundingBox.Empty;
                foreach (var id in new ArgumentReader(args).RequireGuidArray("ids"))
                {
                    var obj = RhinoIds.RequireObject(document, id);
                    var objectBox = obj.Geometry.GetBoundingBox(true);
                    if (objectBox.IsValid)
                        box.Union(objectBox);
                }

                if (!box.IsValid)
                    throw new InvalidOperationException("No valid object bounds were found.");

                view.ActiveViewport.ZoomBoundingBox(box);
                view.Redraw();
                return ToolResponse.Success(new { boundingBox = Box(box) }, "Zoomed active viewport to Rhino object bounds.");
            }));

    private static IMcpTool ZoomToLayer()
        => new DelegateRhinoTool(
            "view_zoom_to_layer",
            "Frames visible Rhino objects on a layer in the active viewport.",
            RhinoSchemas.Object(new Dictionary<string, object>(RhinoSchemas.Props(
                ("layer", "string", "Layer full path or name"))), ["layer"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var layer = reader.RequireString("layer");
                var layerIndex = RhinoObjectFilters.FindLayerIndex(document, layer);
                if (layerIndex < 0)
                    throw new ArgumentException($"Layer not found: {layer}");

                var view = document.Views.ActiveView ?? throw new InvalidOperationException("No active Rhino viewport is available.");
                var settings = RhinoObjectFilters.ActiveObjects(includeHidden: false, includeLocked: true);
                settings.LayerIndexFilter = layerIndex;

                var box = BoundingBox.Empty;
                var count = 0;
                foreach (var obj in document.Objects.GetObjectList(settings))
                {
                    var objectBox = obj.Geometry.GetBoundingBox(true);
                    if (!objectBox.IsValid)
                        continue;

                    box.Union(objectBox);
                    count++;
                }

                if (!box.IsValid)
                    throw new InvalidOperationException($"Layer '{layer}' has no visible object bounds.");

                view.ActiveViewport.ZoomBoundingBox(box);
                view.Redraw();
                return ToolResponse.Success(new { layer, objectCount = count, boundingBox = Box(box) }, $"Zoomed active viewport to layer '{layer}'.");
            }));

    private static IMcpTool SetCamera()
        => new DelegateRhinoTool(
            "view_set_camera",
            "Sets active viewport camera location and target.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["location"] = RhinoSchemas.Point("Camera location"),
                ["target"] = RhinoSchemas.Point("Camera target"),
                ["lensLength"] = RhinoSchemas.Number("Optional camera lens length"),
            }, ["location", "target"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var view = document.Views.ActiveView ?? throw new InvalidOperationException("No active Rhino viewport is available.");
                var viewport = view.ActiveViewport;
                viewport.SetCameraLocations(reader.RequirePoint("target"), reader.RequirePoint("location"));
                if (reader.Has("lensLength"))
                    viewport.Camera35mmLensLength = reader.GetDouble("lensLength");

                view.Redraw();
                return ToolResponse.Success(new
                {
                    camera = new
                    {
                        location = Point(viewport.CameraLocation),
                        target = Point(viewport.CameraTarget),
                    },
                }, "Set active Rhino viewport camera.");
            }));

    private static IMcpTool CaptureSnapshot()
        => new CaptureSnapshotTool();

    private sealed class CaptureSnapshotTool : IMcpTool
    {
        private const int DefaultWidth = 640;
        private const int DefaultHeight = 360;
        private const int MaxWidth = 1280;
        private const int MaxHeight = 720;

        public string Name => "view_capture_snapshot";
        public string Description => "Captures the active Rhino viewport as a JPEG image with scene and camera metadata.";
        public JsonElement InputSchema { get; } = RhinoSchemas.Object(new Dictionary<string, object>
        {
            ["width"] = RhinoSchemas.Integer("Image width in pixels", 64, MaxWidth, DefaultWidth),
            ["height"] = RhinoSchemas.Integer("Image height in pixels", 64, MaxHeight, DefaultHeight),
            ["view"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "top, bottom, left, right, front, back, or perspective" },
            ["displayMode"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Rhino display mode English name, for example Shaded or Rendered" },
            ["zoomExtents"] = RhinoSchemas.Boolean("Frame document extents before capture", defaultValue: false),
        });

        public Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
            => RhinoUiThread.InvokeWithActiveDocumentResultAsync(document =>
            {
                var reader = new ArgumentReader(arguments);
                var width = Math.Clamp(reader.GetInt("width", DefaultWidth), 64, MaxWidth);
                var height = Math.Clamp(reader.GetInt("height", DefaultHeight), 64, MaxHeight);

                var view = document.Views.ActiveView;
                if (view is null)
                    return Error("RHINO_VIEW_NOT_AVAILABLE", "No active Rhino viewport is available.");

                var viewport = view.ActiveViewport;
                ApplyProjection(viewport, reader.GetString("view"));
                ApplyDisplayMode(viewport, reader.GetString("displayMode"));

                if (reader.GetBool("zoomExtents"))
                {
                    var box = SceneBoundingBox(document);
                    if (box.IsValid)
                        viewport.ZoomBoundingBox(box);
                }

                view.Redraw();
                using var bitmap = view.CaptureToBitmap(new Size(width, height));
                if (bitmap is null)
                    return Error("RHINO_VIEW_CAPTURE_FAILED", "Rhino did not return a viewport bitmap.");

                using var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Jpeg);

                var metadata = ToolResponse.Success(new
                {
                    image = new { width, height, mimeType = "image/jpeg" },
                    viewport = new
                    {
                        name = viewport.Name,
                        displayMode = viewport.DisplayMode?.EnglishName,
                        projection = viewport.IsPerspectiveProjection ? "perspective" : "parallel",
                    },
                    camera = new
                    {
                        location = Point(viewport.CameraLocation),
                        target = Point(viewport.CameraTarget),
                        up = Point((Point3d)viewport.CameraUp),
                    },
                    scene = new
                    {
                        boundingBox = SceneBoundingBox(document).IsValid ? Box(SceneBoundingBox(document)) : null,
                    },
                }, "Captured active Rhino viewport.");

                return new McpToolResult
                {
                    Content =
                    {
                        new TextContent(JsonSerializer.Serialize(metadata, McpJson.Default)),
                        new ImageContent(stream.ToArray(), "image/jpeg"),
                    },
                };
            });
    }

    private static BoundingBox SceneBoundingBox(global::Rhino.RhinoDoc document)
    {
        var box = BoundingBox.Empty;
        var settings = new ObjectEnumeratorSettings
        {
            ActiveObjects = true,
            HiddenObjects = false,
            LockedObjects = true,
            DeletedObjects = false,
            IncludeGrips = false,
            IncludeLights = false,
        };

        foreach (var obj in document.Objects.GetObjectList(settings))
        {
            var objectBox = obj.Geometry.GetBoundingBox(true);
            if (objectBox.IsValid)
                box.Union(objectBox);
        }

        return box;
    }

    private static void ApplyProjection(RhinoViewport viewport, string? view)
    {
        if (string.IsNullOrWhiteSpace(view))
            return;

        var projection = view.Trim().ToLowerInvariant() switch
        {
            "top" => DefinedViewportProjection.Top,
            "bottom" => DefinedViewportProjection.Bottom,
            "left" => DefinedViewportProjection.Left,
            "right" => DefinedViewportProjection.Right,
            "front" => DefinedViewportProjection.Front,
            "back" => DefinedViewportProjection.Back,
            "perspective" => DefinedViewportProjection.Perspective,
            _ => DefinedViewportProjection.None,
        };

        if (projection == DefinedViewportProjection.None)
            throw new ArgumentException("Argument 'view' must be one of: top, bottom, left, right, front, back, perspective.");

        viewport.SetProjection(projection, null, true);
    }

    private static void ApplyDisplayMode(RhinoViewport viewport, string? displayMode)
    {
        if (string.IsNullOrWhiteSpace(displayMode))
            return;

        var mode = DisplayModeDescription.GetDisplayModes()
            .FirstOrDefault(item => string.Equals(item.EnglishName, displayMode, StringComparison.OrdinalIgnoreCase));
        if (mode is null)
            throw new ArgumentException($"Display mode was not found: {displayMode}");

        viewport.DisplayMode = mode;
    }

    private static object Box(BoundingBox box)
        => new { min = Point(box.Min), max = Point(box.Max) };

    private static object Point(Point3d point)
        => new { x = point.X, y = point.Y, z = point.Z };

    private static McpToolResult Error(string code, string message)
        => McpToolResult.Error(JsonSerializer.Serialize(ToolResponse.Failure(code, message), McpJson.Default));
}
