using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace Algomim.AutoCad.Mcp.Tools.Common;

internal static class AutoCadDb
{
    public static Point3d Point(double x, double y, double z = 0) => new(x, y, z);
    public static Point2d Point2(double x, double y) => new(x, y);

    public static ObjectId ModelSpace(Database database, Transaction transaction)
    {
        var blockTable = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
        return blockTable[BlockTableRecord.ModelSpace];
    }

    public static BlockTableRecord OpenModelSpace(Database database, Transaction transaction, OpenMode mode)
        => (BlockTableRecord)transaction.GetObject(ModelSpace(database, transaction), mode);

    public static ObjectId ObjectIdFromHandle(Database database, string handle)
    {
        var value = Convert.ToInt64(handle, 16);
        return database.GetObjectId(false, new Handle(value), 0);
    }

    public static IReadOnlyList<ObjectId> ObjectIdsFromHandles(Database database, IEnumerable<string> handles)
        => handles.Select(handle => ObjectIdFromHandle(database, handle)).ToArray();

    public static string HandleOf(DBObject obj) => obj.Handle.Value.ToString("X");

    public static void SetLayer(Autodesk.AutoCAD.DatabaseServices.Entity entity, string? layerName)
    {
        if (!string.IsNullOrWhiteSpace(layerName))
            entity.Layer = layerName;
    }

    public static void SetColor(Autodesk.AutoCAD.DatabaseServices.Entity entity, int? colorIndex)
    {
        if (colorIndex.HasValue)
            entity.Color = Color.FromColorIndex(ColorMethod.ByAci, Convert.ToInt16(colorIndex.Value));
    }

    public static void EnsureLayer(Database database, Transaction transaction, string? name, int? colorIndex = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        var layers = (LayerTable)transaction.GetObject(database.LayerTableId, OpenMode.ForRead);
        if (layers.Has(name)) return;

        layers.UpgradeOpen();
        var record = new LayerTableRecord { Name = name };
        if (colorIndex.HasValue)
            record.Color = Color.FromColorIndex(ColorMethod.ByAci, Convert.ToInt16(colorIndex.Value));
        layers.Add(record);
        transaction.AddNewlyCreatedDBObject(record, true);
    }

    public static string EntityType(Autodesk.AutoCAD.DatabaseServices.Entity entity)
    {
        var type = entity.GetType().Name;
        return type.EndsWith("Entity", StringComparison.Ordinal) ? type[..^6] : type;
    }

    public static object EntitySummary(Autodesk.AutoCAD.DatabaseServices.Entity entity)
    {
        object? extents = null;
        try
        {
            var bounds = entity.GeometricExtents;
            extents = new
            {
                min = new { x = bounds.MinPoint.X, y = bounds.MinPoint.Y, z = bounds.MinPoint.Z },
                max = new { x = bounds.MaxPoint.X, y = bounds.MaxPoint.Y, z = bounds.MaxPoint.Z },
            };
        }
        catch
        {
            // Not every entity has geometric extents before regen.
        }

        return new
        {
            handle = HandleOf(entity),
            type = EntityType(entity),
            layer = entity.Layer,
            colorIndex = entity.Color.ColorIndex,
            extents,
        };
    }

    public static void AddEntity(BlockTableRecord modelSpace, Transaction transaction, Autodesk.AutoCAD.DatabaseServices.Entity entity)
    {
        modelSpace.AppendEntity(entity);
        transaction.AddNewlyCreatedDBObject(entity, true);
    }
}
