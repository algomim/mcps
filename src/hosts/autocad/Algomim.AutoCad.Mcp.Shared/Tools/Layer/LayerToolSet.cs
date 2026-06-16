using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Algomim.Aec.Mcp.Tooling;
using Algomim.AutoCad.Mcp.Harness;
using Algomim.AutoCad.Mcp.Tools.Common;

namespace Algomim.AutoCad.Mcp.Tools.Layer;

internal static class LayerToolSet
{
    public static IEnumerable<IMcpTool> Create(AutoCadToolServices services)
    {
        yield return new DelegateAutoCadTool(
            "layer_list",
            "Lists AutoCAD layers with core display/state properties.",
            AutoCadSchemas.Empty,
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    var layers = (LayerTable)transaction.GetObject(database.LayerTableId, OpenMode.ForRead);
                    return layers
                        .Cast<ObjectId>()
                        .Select(id => (LayerTableRecord)transaction.GetObject(id, OpenMode.ForRead))
                        .Select(record => new
                        {
                            name = record.Name,
                            colorIndex = record.Color.ColorIndex,
                            isCurrent = record.ObjectId == database.Clayer,
                            isOff = record.IsOff,
                            isFrozen = record.IsFrozen,
                            isLocked = record.IsLocked,
                            lineWeight = record.LineWeight.ToString(),
                        })
                        .OrderBy(layer => layer.name, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                });

                return ToolResponse.Success(data, $"Listed {data.Length} layers.");
            }));

        yield return new DelegateAutoCadTool(
            "layer_create",
            "Creates an AutoCAD layer if it does not already exist.",
            AutoCadSchemas.Object(
                AutoCadSchemas.Props(
                    ("name", "string", "Layer name"),
                    ("colorIndex", "number", "ACI color index"),
                    ("lineWeight", "string", "AutoCAD LineWeight enum name, for example LineWeight025")),
                ["name"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var name = reader.RequireString("name");
                var colorIndex = reader.GetInt("colorIndex", 7);
                var lineWeight = reader.GetString("lineWeight");

                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    var layers = (LayerTable)transaction.GetObject(database.LayerTableId, OpenMode.ForRead);
                    if (layers.Has(name))
                    {
                        return new { name, created = false };
                    }

                    layers.UpgradeOpen();
                    var record = new LayerTableRecord
                    {
                        Name = name,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, Convert.ToInt16(colorIndex)),
                    };

                    if (!string.IsNullOrWhiteSpace(lineWeight) &&
                        Enum.TryParse<LineWeight>(lineWeight, ignoreCase: true, out var parsed))
                    {
                        record.LineWeight = parsed;
                    }

                    layers.Add(record);
                    transaction.AddNewlyCreatedDBObject(record, true);
                    return new { name, created = true };
                });

                return ToolResponse.Success(data, data.created ? $"Created layer '{name}'." : $"Layer '{name}' already exists.");
            }));

        yield return new DelegateAutoCadTool(
            "layer_set_current",
            "Sets the current AutoCAD layer.",
            AutoCadSchemas.Object(AutoCadSchemas.Props(("name", "string", "Layer name")), ["name"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var name = new ArgumentReader(args).RequireString("name");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    var layers = (LayerTable)transaction.GetObject(database.LayerTableId, OpenMode.ForRead);
                    if (!layers.Has(name))
                        throw new ArgumentException($"Layer '{name}' was not found.");

                    database.Clayer = layers[name];
                    return new { currentLayer = name };
                });

                return ToolResponse.Success(data, $"Current layer set to '{name}'.");
            }));

        yield return new DelegateAutoCadTool(
            "layer_set_properties",
            "Updates layer color, line weight, and state properties.",
            AutoCadSchemas.Object(
                AutoCadSchemas.Props(
                    ("name", "string", "Layer name"),
                    ("colorIndex", "number", "ACI color index"),
                    ("lineWeight", "string", "AutoCAD LineWeight enum name"),
                    ("isOff", "boolean", "Whether the layer is off"),
                    ("isFrozen", "boolean", "Whether the layer is frozen"),
                    ("isLocked", "boolean", "Whether the layer is locked")),
                ["name"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var name = reader.RequireString("name");

                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    var layers = (LayerTable)transaction.GetObject(database.LayerTableId, OpenMode.ForRead);
                    if (!layers.Has(name))
                        throw new ArgumentException($"Layer '{name}' was not found.");

                    var record = (LayerTableRecord)transaction.GetObject(layers[name], OpenMode.ForWrite);
                    if (reader.Has("colorIndex"))
                        record.Color = Color.FromColorIndex(ColorMethod.ByAci, Convert.ToInt16(reader.GetInt("colorIndex", 7)));
                    if (reader.GetBool("isOff") is { } isOff)
                        record.IsOff = isOff;
                    if (reader.GetBool("isFrozen") is { } isFrozen)
                        record.IsFrozen = isFrozen;
                    if (reader.GetBool("isLocked") is { } isLocked)
                        record.IsLocked = isLocked;
                    if (reader.GetString("lineWeight") is { Length: > 0 } lineWeight &&
                        Enum.TryParse<LineWeight>(lineWeight, ignoreCase: true, out var parsed))
                    {
                        record.LineWeight = parsed;
                    }

                    return new
                    {
                        name,
                        colorIndex = record.Color.ColorIndex,
                        record.IsOff,
                        record.IsFrozen,
                        record.IsLocked,
                        lineWeight = record.LineWeight.ToString(),
                    };
                });

                return ToolResponse.Success(data, $"Updated layer '{name}'.");
            }));

        yield return new DelegateAutoCadTool(
            "layer_delete",
            "Deletes an unused non-current AutoCAD layer.",
            AutoCadSchemas.Object(AutoCadSchemas.Props(("name", "string", "Layer name")), ["name"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var name = new ArgumentReader(args).RequireString("name");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    if (name is "0" or "Defpoints")
                        throw new ArgumentException("Layer '0' and 'Defpoints' cannot be deleted.");

                    var layers = (LayerTable)transaction.GetObject(database.LayerTableId, OpenMode.ForRead);
                    if (!layers.Has(name))
                        throw new ArgumentException($"Layer '{name}' was not found.");
                    if (layers[name] == database.Clayer)
                        throw new ArgumentException("Current layer cannot be deleted.");

                    var record = (LayerTableRecord)transaction.GetObject(layers[name], OpenMode.ForWrite);
                    record.Erase();
                    return new { name, deleted = true };
                });

                return ToolResponse.Success(data, $"Deleted layer '{name}'.");
            }));
    }
}
