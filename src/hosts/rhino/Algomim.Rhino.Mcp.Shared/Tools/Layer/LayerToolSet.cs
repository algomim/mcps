using Algomim.Aec.Mcp.Tooling;
using Algomim.Rhino.Mcp.Tools.Common;
using Rhino.DocObjects;

namespace Algomim.Rhino.Mcp.Tools.Layer;

internal static class LayerToolSet
{
    public static IEnumerable<IMcpTool> Create()
    {
        yield return List();
        yield return CreateLayer();
        yield return SetCurrent();
        yield return Delete();
    }

    private static IMcpTool List()
        => new DelegateRhinoTool(
            "layer_list",
            "Lists Rhino document layers.",
            RhinoSchemas.Empty,
            _ => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var layers = Enumerable.Range(0, document.Layers.Count)
                    .Select(index => document.Layers[index])
                    .Where(layer => layer is not null)
                    .Select(layer => new
                    {
                        index = layer.Index,
                        name = layer.Name,
                        fullPath = layer.FullPath,
                        isCurrent = layer.Index == document.Layers.CurrentLayerIndex,
                        isDeleted = layer.IsDeleted,
                        isReference = layer.IsReference,
                    })
                    .ToArray();

                return ToolResponse.Success(new { count = layers.Length, layers }, $"Found {layers.Length} Rhino layer(s).");
            }));

    private static IMcpTool CreateLayer()
        => new DelegateRhinoTool(
            "layer_create",
            "Creates a Rhino layer if it does not already exist.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["name"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Layer name" },
                ["color"] = RhinoSchemas.Color("Layer color as RGB"),
            }, ["name"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var name = reader.RequireString("name");
                var existing = RhinoObjectFilters.FindLayerIndex(document, name);
                if (existing >= 0)
                    return ToolResponse.Success(LayerSummary(document.Layers[existing]), $"Layer '{name}' already exists.");

                var layer = new global::Rhino.DocObjects.Layer { Name = name };
                var color = reader.GetColor("color");
                if (color.HasValue)
                    layer.Color = color.Value;

                var index = document.Layers.Add(layer);
                if (index < 0)
                    throw new InvalidOperationException($"Could not create layer '{name}'.");

                return ToolResponse.Success(LayerSummary(document.Layers[index]), $"Created layer '{name}'.");
            }));

    private static IMcpTool SetCurrent()
        => new DelegateRhinoTool(
            "layer_set_current",
            "Sets the active Rhino layer, creating it if needed.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["name"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Layer name" },
            }, ["name"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var name = new ArgumentReader(args).RequireString("name");
                var index = RhinoAttributes.EnsureLayer(document, name);
                document.Layers.SetCurrentLayerIndex(index, true);
                return ToolResponse.Success(LayerSummary(document.Layers[index]), $"Set current layer to '{name}'.");
            }));

    private static IMcpTool Delete()
        => new DelegateRhinoTool(
            "layer_delete",
            "Deletes a Rhino layer by name.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["name"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Layer name" },
            }, ["name"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var name = new ArgumentReader(args).RequireString("name");
                var index = RhinoObjectFilters.FindLayerIndex(document, name);
                if (index < 0)
                    throw new ArgumentException($"Layer was not found: {name}");

                if (index == document.Layers.CurrentLayerIndex)
                    throw new ArgumentException("Cannot delete the current Rhino layer.");

                var deleted = document.Layers.Delete(index, true);
                return ToolResponse.Success(new { deleted, name }, deleted ? $"Deleted layer '{name}'." : $"Layer '{name}' was not deleted.");
            }));

    private static object LayerSummary(global::Rhino.DocObjects.Layer layer)
        => new
        {
            index = layer.Index,
            name = layer.Name,
            fullPath = layer.FullPath,
            isDeleted = layer.IsDeleted,
            isReference = layer.IsReference,
        };
}
