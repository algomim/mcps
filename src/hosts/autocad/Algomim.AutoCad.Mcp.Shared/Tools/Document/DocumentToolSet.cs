using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Algomim.Aec.Mcp.Tooling;
using Algomim.AutoCad.Mcp.Harness;
using Algomim.AutoCad.Mcp.Tools.Common;

namespace Algomim.AutoCad.Mcp.Tools.Document;

internal static class DocumentToolSet
{
    public static IEnumerable<IMcpTool> Create(AutoCadToolServices services)
    {
        yield return Save(services);
        yield return SaveAs(services);
        yield return ZoomExtents(services);
        yield return Purge(services);
    }

    private static IMcpTool Save(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "document_save",
            "Saves the current drawing to its current file path.",
            AutoCadSchemas.Empty,
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var fileName = document.Database.Filename;
                if (string.IsNullOrWhiteSpace(fileName))
                    throw new ArgumentException("Drawing has no file name. Use document_save_as.");

                document.Database.SaveAs(fileName, DwgVersion.Current);
                return ToolResponse.Success(new { path = fileName }, "Saved drawing.");
            }));

    private static IMcpTool SaveAs(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "document_save_as",
            "Saves the current drawing to a target DWG file.",
            AutoCadSchemas.Object(AutoCadSchemas.Props(("path", "string", "Target DWG path")), ["path"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var path = new ArgumentReader(args).RequireString("path");
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
                document.Database.SaveAs(path, DwgVersion.Current);
                return ToolResponse.Success(new { path }, "Saved drawing as new file.");
            }));

    private static IMcpTool ZoomExtents(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "document_zoom_extents",
            "Sets the active editor view to drawing extents.",
            AutoCadSchemas.Empty,
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var database = document.Database;
                var editor = document.Editor;
                database.UpdateExt(true);

                var extents = database.Extmax.X > database.Extmin.X && database.Extmax.Y > database.Extmin.Y
                    ? new Extents2d(database.Extmin.X, database.Extmin.Y, database.Extmax.X, database.Extmax.Y)
                    : new Extents2d(-10, -10, 10, 10);

                var view = editor.GetCurrentView();
                view.CenterPoint = new Autodesk.AutoCAD.Geometry.Point2d(
                    (extents.MinPoint.X + extents.MaxPoint.X) / 2,
                    (extents.MinPoint.Y + extents.MaxPoint.Y) / 2);
                view.Width = Math.Max(1, extents.MaxPoint.X - extents.MinPoint.X);
                view.Height = Math.Max(1, extents.MaxPoint.Y - extents.MinPoint.Y);
                editor.SetCurrentView(view);

                return ToolResponse.Success(new { view.Width, view.Height }, "Zoomed to extents.");
            }));

    private static IMcpTool Purge(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "document_purge",
            "Purges unused symbol-table records from the drawing database.",
            AutoCadSchemas.Empty,
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    var ids = new ObjectIdCollection();
                    AddTableIds(transaction, database.LayerTableId, ids);
                    AddTableIds(transaction, database.BlockTableId, ids);
                    AddTableIds(transaction, database.LinetypeTableId, ids);
                    AddTableIds(transaction, database.TextStyleTableId, ids);
                    AddTableIds(transaction, database.DimStyleTableId, ids);

                    database.Purge(ids);
                    var purged = 0;
                    foreach (ObjectId id in ids)
                    {
                        if (id.IsErased) continue;
                        var obj = transaction.GetObject(id, OpenMode.ForWrite, false);
                        if (obj is null) continue;
                        obj.Erase();
                        purged++;
                    }

                    return new { purged };
                });
                return ToolResponse.Success(data, $"Purged {data.purged} unused records.");
            }));

    private static void AddTableIds(Transaction transaction, ObjectId tableId, ObjectIdCollection ids)
    {
        if (transaction.GetObject(tableId, OpenMode.ForRead) is not SymbolTable table) return;
        foreach (ObjectId id in table)
            ids.Add(id);
    }
}
