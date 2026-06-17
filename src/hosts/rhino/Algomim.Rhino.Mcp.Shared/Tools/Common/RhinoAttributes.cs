using Rhino;
using Rhino.DocObjects;

namespace Algomim.Rhino.Mcp.Tools.Common;

internal static class RhinoAttributes
{
    public static ObjectAttributes FromArguments(RhinoDoc document, ArgumentReader reader)
    {
        var attributes = new ObjectAttributes();
        var name = reader.GetString("name");
        if (!string.IsNullOrWhiteSpace(name))
            attributes.Name = name;

        var layer = reader.GetString("layer");
        if (!string.IsNullOrWhiteSpace(layer))
            attributes.LayerIndex = EnsureLayer(document, layer);

        var color = reader.GetColor("color");
        if (color.HasValue)
        {
            attributes.ColorSource = ObjectColorSource.ColorFromObject;
            attributes.ObjectColor = color.Value;
        }

        return attributes;
    }

    public static void ApplyMutableAttributes(RhinoDoc document, RhinoObject obj, ArgumentReader reader)
    {
        var attributes = obj.Attributes.Duplicate();
        var modified = false;

        var name = reader.GetString("name") ?? reader.GetString("newName");
        if (!string.IsNullOrWhiteSpace(name))
        {
            attributes.Name = name;
            modified = true;
        }

        var layer = reader.GetString("layer");
        if (!string.IsNullOrWhiteSpace(layer))
        {
            attributes.LayerIndex = EnsureLayer(document, layer);
            modified = true;
        }

        var color = reader.GetColor("color");
        if (color.HasValue)
        {
            attributes.ColorSource = ObjectColorSource.ColorFromObject;
            attributes.ObjectColor = color.Value;
            modified = true;
        }

        if (modified)
            document.Objects.ModifyAttributes(obj, attributes, true);
    }

    public static int EnsureLayer(RhinoDoc document, string layerNameOrPath)
    {
        var existing = RhinoObjectFilters.FindLayerIndex(document, layerNameOrPath);
        if (existing >= 0)
            return existing;

        var layer = new global::Rhino.DocObjects.Layer { Name = layerNameOrPath };
        var index = document.Layers.Add(layer);
        if (index < 0)
            throw new InvalidOperationException($"Could not create layer '{layerNameOrPath}'.");

        return index;
    }
}
