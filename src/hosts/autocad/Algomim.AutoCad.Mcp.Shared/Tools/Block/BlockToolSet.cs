using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Algomim.Aec.Mcp.Tooling;
using Algomim.AutoCad.Mcp.Harness;
using Algomim.AutoCad.Mcp.Tools.Common;

namespace Algomim.AutoCad.Mcp.Tools.Block;

internal static class BlockToolSet
{
    public static IEnumerable<IMcpTool> Create(AutoCadToolServices services)
    {
        yield return List(services);
        yield return Insert(services);
        yield return Explode(services);
        yield return GetAttributes(services);
        yield return SetAttribute(services);
        yield return CreateDefinition(services);
    }

    private static IMcpTool List(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "block_list",
            "Lists block definitions in the current drawing.",
            AutoCadSchemas.Empty,
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    var table = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
                    var blocks = table
                        .Cast<ObjectId>()
                        .Select(id => (BlockTableRecord)transaction.GetObject(id, OpenMode.ForRead))
                        .Select(record => new
                        {
                            name = record.Name,
                            isAnonymous = record.IsAnonymous,
                            isLayout = record.IsLayout,
                            origin = new { x = record.Origin.X, y = record.Origin.Y, z = record.Origin.Z },
                        })
                        .OrderBy(block => block.name, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    return new { count = blocks.Length, blocks };
                });
                return ToolResponse.Success(data, $"Listed {data.count} block definitions.");
            }));

    private static IMcpTool Insert(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "block_insert",
            "Inserts an existing block definition or imports one from a DWG file.",
            AutoCadSchemas.Object(
                AutoCadSchemas.Props(
                    ("blockName", "string", "Block definition name"),
                    ("blockPath", "string", "Optional DWG file path to import"),
                    ("x", "number", "Insertion X"),
                    ("y", "number", "Insertion Y"),
                    ("z", "number", "Insertion Z"),
                    ("scale", "number", "Uniform scale"),
                    ("rotation", "number", "Rotation in degrees"),
                    ("layer", "string", "Target layer")),
                ["blockName", "x", "y"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var blockName = reader.RequireString("blockName");
                var blockPath = reader.GetString("blockPath");
                var layer = reader.GetString("layer");

                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    AutoCadDb.EnsureLayer(database, transaction, layer);
                    var table = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
                    ObjectId blockId;

                    if (table.Has(blockName))
                    {
                        blockId = table[blockName];
                    }
                    else if (!string.IsNullOrWhiteSpace(blockPath) && File.Exists(blockPath))
                    {
                        using var source = new Database(false, true);
                        source.ReadDwgFile(blockPath, FileOpenMode.OpenForReadAndAllShare, true, string.Empty);
                        blockId = database.Insert(blockName, source, false);
                    }
                    else
                    {
                        throw new ArgumentException($"Block '{blockName}' was not found and no valid blockPath was supplied.");
                    }

                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForWrite);
                    var reference = new BlockReference(
                        AutoCadDb.Point(reader.RequireDouble("x"), reader.RequireDouble("y"), reader.GetDouble("z")),
                        blockId)
                    {
                        ScaleFactors = new Scale3d(reader.GetDouble("scale", 1)),
                        Rotation = DegreesToRadians(reader.GetDouble("rotation")),
                    };
                    AutoCadDb.SetLayer(reference, layer);
                    AutoCadDb.AddEntity(model, transaction, reference);
                    return AutoCadDb.EntitySummary(reference);
                });

                return ToolResponse.Success(data, $"Inserted block '{blockName}'.");
            }));

    private static IMcpTool Explode(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "block_explode",
            "Explodes a block reference into model space entities.",
            AutoCadSchemas.Object(
                AutoCadSchemas.Props(
                    ("handle", "string", "Block reference handle"),
                    ("deleteOriginal", "boolean", "Erase the source block reference")),
                ["handle"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var handle = reader.RequireString("handle");
                var deleteOriginal = reader.GetBool("deleteOriginal") ?? true;
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    var reference = transaction.GetObject(AutoCadDb.ObjectIdFromHandle(database, handle), OpenMode.ForWrite) as BlockReference;
                    if (reference is null)
                        throw new ArgumentException("Entity is not a block reference.");

                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForWrite);
                    var exploded = new DBObjectCollection();
                    reference.Explode(exploded);
                    var entities = new List<object>();
                    foreach (DBObject obj in exploded)
                    {
                        if (obj is not Autodesk.AutoCAD.DatabaseServices.Entity entity) continue;
                        AutoCadDb.AddEntity(model, transaction, entity);
                        entities.Add(AutoCadDb.EntitySummary(entity));
                    }

                    if (deleteOriginal)
                        reference.Erase();

                    return new { count = entities.Count, entities, deletedOriginal = deleteOriginal };
                });
                return ToolResponse.Success(data, $"Exploded block into {data.count} entities.");
            }));

    private static IMcpTool GetAttributes(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "block_get_attributes",
            "Gets attributes from a block reference.",
            AutoCadSchemas.Object(AutoCadSchemas.Props(("handle", "string", "Block reference handle")), ["handle"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var handle = new ArgumentReader(args).RequireString("handle");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    var reference = transaction.GetObject(AutoCadDb.ObjectIdFromHandle(database, handle), OpenMode.ForRead) as BlockReference;
                    if (reference is null)
                        throw new ArgumentException("Entity is not a block reference.");

                    var attributes = reference.AttributeCollection
                        .Cast<ObjectId>()
                        .Select(id => (AttributeReference)transaction.GetObject(id, OpenMode.ForRead))
                        .Select(attribute => new { tag = attribute.Tag, text = attribute.TextString, handle = AutoCadDb.HandleOf(attribute) })
                        .ToArray();
                    return new { blockName = reference.Name, attributes };
                });
                return ToolResponse.Success(data, "Read block attributes.");
            }));

    private static IMcpTool SetAttribute(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "block_set_attribute",
            "Sets a block reference attribute by tag.",
            AutoCadSchemas.Object(
                AutoCadSchemas.Props(
                    ("handle", "string", "Block reference handle"),
                    ("tag", "string", "Attribute tag"),
                    ("text", "string", "New attribute text")),
                ["handle", "tag", "text"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var handle = reader.RequireString("handle");
                var tag = reader.RequireString("tag");
                var text = reader.RequireString("text");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    var reference = transaction.GetObject(AutoCadDb.ObjectIdFromHandle(database, handle), OpenMode.ForRead) as BlockReference;
                    if (reference is null)
                        throw new ArgumentException("Entity is not a block reference.");

                    foreach (ObjectId id in reference.AttributeCollection)
                    {
                        var attribute = (AttributeReference)transaction.GetObject(id, OpenMode.ForWrite);
                        if (!string.Equals(attribute.Tag, tag, StringComparison.OrdinalIgnoreCase))
                            continue;

                        attribute.TextString = text;
                        return new { updated = true, tag, text };
                    }

                    throw new ArgumentException($"Attribute tag '{tag}' was not found.");
                });
                return ToolResponse.Success(data, $"Updated attribute '{tag}'.");
            }));

    private static IMcpTool CreateDefinition(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "block_create_definition",
            "Creates a block definition by cloning existing entities.",
            AutoCadSchemas.Object(
                new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["blockName"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "New block definition name" },
                    ["handles"] = AutoCadSchemas.StringArray("Entity handles to clone into the block definition"),
                    ["baseX"] = new Dictionary<string, object> { ["type"] = "number", ["description"] = "Block base point X" },
                    ["baseY"] = new Dictionary<string, object> { ["type"] = "number", ["description"] = "Block base point Y" },
                    ["baseZ"] = new Dictionary<string, object> { ["type"] = "number", ["description"] = "Block base point Z" },
                },
                ["blockName", "handles"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var blockName = reader.RequireString("blockName");
                var handles = reader.RequireStringArray("handles");
                var basePoint = AutoCadDb.Point(reader.GetDouble("baseX"), reader.GetDouble("baseY"), reader.GetDouble("baseZ"));

                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    var table = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForWrite);
                    if (table.Has(blockName))
                        throw new ArgumentException($"Block '{blockName}' already exists.");

                    var definition = new BlockTableRecord { Name = blockName, Origin = basePoint };
                    table.Add(definition);
                    transaction.AddNewlyCreatedDBObject(definition, true);

                    var transformToOrigin = Matrix3d.Displacement(basePoint.GetVectorTo(Point3d.Origin));
                    var cloned = 0;
                    foreach (var handle in handles)
                    {
                        var source = transaction.GetObject(AutoCadDb.ObjectIdFromHandle(database, handle), OpenMode.ForRead) as Autodesk.AutoCAD.DatabaseServices.Entity;
                        if (source is null) continue;

                        var clone = (Autodesk.AutoCAD.DatabaseServices.Entity)source.Clone();
                        clone.TransformBy(transformToOrigin);
                        definition.AppendEntity(clone);
                        transaction.AddNewlyCreatedDBObject(clone, true);
                        cloned++;
                    }

                    return new { blockName, cloned };
                });
                return ToolResponse.Success(data, $"Created block definition '{blockName}' with {data.cloned} entities.");
            }));

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
