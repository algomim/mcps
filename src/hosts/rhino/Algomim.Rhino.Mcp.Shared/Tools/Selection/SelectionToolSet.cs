using Algomim.Aec.Mcp.Tooling;
using Algomim.Rhino.Mcp.Tools.Common;

namespace Algomim.Rhino.Mcp.Tools.Selection;

internal static class SelectionToolSet
{
    private const int DefaultLimit = 100;
    private const int MaxLimit = 500;

    public static IEnumerable<IMcpTool> Create()
    {
        yield return Get();
        yield return Set();
        yield return Clear();
    }

    private static IMcpTool Get()
        => new DelegateRhinoTool(
            "selection_get",
            "Gets the current Rhino selection without changing the document.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["limit"] = RhinoSchemas.Integer("Maximum number of selected objects to return", 1, MaxLimit, DefaultLimit),
            }),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var limit = Math.Clamp(reader.GetInt("limit", DefaultLimit), 1, MaxLimit);
                var selected = document.Objects.GetSelectedObjects(false, false).ToArray();
                var objects = selected
                    .Take(limit)
                    .Select(obj => RhinoObjectSummary.From(document, obj))
                    .ToArray();

                var data = new
                {
                    count = selected.Length,
                    returned = objects.Length,
                    truncated = selected.Length > objects.Length,
                    objects,
                };

                return ToolResponse.Success(data, $"Read {selected.Length} selected Rhino object(s).");
            }));

    private static IMcpTool Set()
        => new DelegateRhinoTool(
            "selection_set",
            "Selects Rhino objects by IDs, exact names, layer, or geometry type.",
            RhinoSchemas.Object(new Dictionary<string, object>(RhinoSchemas.Props(
                ("layer", "string", "Layer full path or name filter"),
                ("geometryType", "string", "Geometry filter: point, pointset, curve, surface, brep, mesh, annotation, light, block"),
                ("clearExisting", "boolean", "Clear current selection first")))
            {
                ["ids"] = RhinoSchemas.StringArray("Object GUIDs"),
                ["names"] = RhinoSchemas.StringArray("Exact object names"),
            }),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                if (reader.GetBool("clearExisting", fallback: true))
                    document.Objects.UnselectAll();

                var selected = 0;
                foreach (var idText in reader.GetStringArray("ids"))
                {
                    if (!Guid.TryParse(idText, out var id))
                        throw new ArgumentException($"Invalid object GUID: {idText}");

                    var obj = document.Objects.FindId(id);
                    if (obj is not null && obj.Select(true) > 0)
                        selected++;
                }

                var names = reader.GetStringArray("names").ToHashSet(StringComparer.Ordinal);
                var layer = reader.GetString("layer");
                var geometryType = reader.GetString("geometryType");
                if (names.Count > 0 || !string.IsNullOrWhiteSpace(layer) || !string.IsNullOrWhiteSpace(geometryType))
                {
                    var settings = RhinoObjectFilters.ActiveObjects(includeHidden: false, includeLocked: true);
                    if (!RhinoObjectFilters.TryApplyGeometryType(settings, geometryType))
                        throw new ArgumentException("Argument 'geometryType' must be one of: point, pointset, curve, surface, brep, mesh, annotation, light, block.");

                    if (!string.IsNullOrWhiteSpace(layer))
                    {
                        var layerIndex = RhinoObjectFilters.FindLayerIndex(document, layer);
                        if (layerIndex < 0)
                            throw new ArgumentException($"Layer was not found: {layer}");

                        settings.LayerIndexFilter = layerIndex;
                    }

                    foreach (var obj in document.Objects.GetObjectList(settings))
                    {
                        if (names.Count > 0 && !names.Contains(obj.Name ?? string.Empty))
                            continue;

                        if (obj.Select(true) > 0)
                            selected++;
                    }
                }

                document.Views.Redraw();
                return ToolResponse.Success(new { selected }, $"Selected {selected} Rhino object(s).");
            }));

    private static IMcpTool Clear()
        => new DelegateRhinoTool(
            "selection_clear",
            "Clears the current Rhino selection.",
            RhinoSchemas.Empty,
            _ => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var changed = document.Objects.UnselectAll();
                document.Views.Redraw();
                return ToolResponse.Success(new { changed }, "Cleared Rhino selection.");
            }));
}
