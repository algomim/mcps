using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Algomim.Aec.Mcp.Tooling;
using Algomim.AutoCad.Mcp.Harness;
using Algomim.AutoCad.Mcp.Tools.Common;

namespace Algomim.AutoCad.Mcp.Tools.Annotation;

internal static class AnnotationToolSet
{
    public static IEnumerable<IMcpTool> Create(AutoCadToolServices services)
    {
        yield return CreateText(services);
        yield return CreateMText(services);
        yield return CreateLeader(services);
        yield return CreateTable(services);
        yield return UpdateText(services);
    }

    private static IMcpTool CreateText(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "annotation_create_text",
            "Creates single-line DBText.",
            TextSchema(),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var layer = reader.GetString("layer");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    AutoCadDb.EnsureLayer(database, transaction, layer);
                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForWrite);
                    var text = new DBText
                    {
                        Position = AutoCadDb.Point(reader.RequireDouble("x"), reader.RequireDouble("y"), reader.GetDouble("z")),
                        TextString = reader.RequireString("text"),
                        Height = reader.GetDouble("height", 2.5),
                        Rotation = DegreesToRadians(reader.GetDouble("rotation")),
                    };
                    AutoCadDb.SetLayer(text, layer);
                    AutoCadDb.AddEntity(model, transaction, text);
                    return AutoCadDb.EntitySummary(text);
                });
                return ToolResponse.Success(data, "Created text.");
            }));

    private static IMcpTool CreateMText(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "annotation_create_mtext",
            "Creates multiline MText.",
            TextSchema(extraWidth: true),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var layer = reader.GetString("layer");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    AutoCadDb.EnsureLayer(database, transaction, layer);
                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForWrite);
                    var text = new MText
                    {
                        Location = AutoCadDb.Point(reader.RequireDouble("x"), reader.RequireDouble("y"), reader.GetDouble("z")),
                        Contents = reader.RequireString("text"),
                        TextHeight = reader.GetDouble("height", 2.5),
                        Width = reader.GetDouble("width", 20),
                        Rotation = DegreesToRadians(reader.GetDouble("rotation")),
                    };
                    AutoCadDb.SetLayer(text, layer);
                    AutoCadDb.AddEntity(model, transaction, text);
                    return AutoCadDb.EntitySummary(text);
                });
                return ToolResponse.Success(data, "Created mtext.");
            }));

    private static IMcpTool CreateLeader(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "annotation_create_leader",
            "Creates a leader polyline and optional text note.",
            AutoCadSchemas.Object(
                new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["points"] = AutoCadSchemas.PointArray("Leader vertices"),
                    ["text"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Optional note text" },
                    ["height"] = new Dictionary<string, object> { ["type"] = "number", ["description"] = "Text height" },
                    ["layer"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Target layer" },
                },
                ["points"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var points = reader.RequirePointArray("points");
                if (points.Count < 2) throw new ArgumentException("At least two leader points are required.");
                var layer = reader.GetString("layer");
                var note = reader.GetString("text");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    AutoCadDb.EnsureLayer(database, transaction, layer);
                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForWrite);
                    var leader = new Leader();
                    foreach (var point in points)
                        leader.AppendVertex(AutoCadDb.Point(point.X, point.Y, point.Z));
                    AutoCadDb.SetLayer(leader, layer);
                    AutoCadDb.AddEntity(model, transaction, leader);

                    object? textSummary = null;
                    if (!string.IsNullOrWhiteSpace(note))
                    {
                        var last = points[^1];
                        var text = new DBText
                        {
                            Position = AutoCadDb.Point(last.X, last.Y, last.Z),
                            TextString = note,
                            Height = reader.GetDouble("height", 2.5),
                        };
                        AutoCadDb.SetLayer(text, layer);
                        AutoCadDb.AddEntity(model, transaction, text);
                        textSummary = AutoCadDb.EntitySummary(text);
                    }

                    return new { leader = AutoCadDb.EntitySummary(leader), text = textSummary };
                });
                return ToolResponse.Success(data, "Created leader.");
            }));

    private static IMcpTool CreateTable(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "annotation_create_table",
            "Creates a simple AutoCAD table.",
            AutoCadSchemas.Object(
                new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["x"] = new Dictionary<string, object> { ["type"] = "number", ["description"] = "Insertion X" },
                    ["y"] = new Dictionary<string, object> { ["type"] = "number", ["description"] = "Insertion Y" },
                    ["rows"] = new Dictionary<string, object> { ["type"] = "number", ["description"] = "Row count" },
                    ["columns"] = new Dictionary<string, object> { ["type"] = "number", ["description"] = "Column count" },
                    ["rowHeight"] = new Dictionary<string, object> { ["type"] = "number", ["description"] = "Row height" },
                    ["columnWidth"] = new Dictionary<string, object> { ["type"] = "number", ["description"] = "Column width" },
                    ["values"] = new Dictionary<string, object> { ["type"] = "array", ["description"] = "Flat string values, row-major" },
                    ["layer"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Target layer" },
                },
                ["x", "y", "rows", "columns"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var layer = reader.GetString("layer");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    AutoCadDb.EnsureLayer(database, transaction, layer);
                    var model = AutoCadDb.OpenModelSpace(database, transaction, OpenMode.ForWrite);
                    var rows = Math.Max(1, reader.GetInt("rows", 1));
                    var columns = Math.Max(1, reader.GetInt("columns", 1));
                    var table = new Table
                    {
                        Position = AutoCadDb.Point(reader.RequireDouble("x"), reader.RequireDouble("y"), reader.GetDouble("z")),
                    };
                    table.SetSize(rows, columns);
                    table.SetRowHeight(reader.GetDouble("rowHeight", 3));
                    table.SetColumnWidth(reader.GetDouble("columnWidth", 12));

                    if (args.HasValue && args.Value.TryGetProperty("values", out var values) && values.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var flat = values.EnumerateArray().Select(item => item.GetString() ?? item.GetRawText()).ToArray();
                        for (var index = 0; index < flat.Length && index < rows * columns; index++)
                        {
                            table.Cells[index / columns, index % columns].TextString = flat[index];
                        }
                    }

                    AutoCadDb.SetLayer(table, layer);
                    AutoCadDb.AddEntity(model, transaction, table);
                    return AutoCadDb.EntitySummary(table);
                });
                return ToolResponse.Success(data, "Created table.");
            }));

    private static IMcpTool UpdateText(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "annotation_update_text",
            "Updates DBText or MText content.",
            AutoCadSchemas.Object(
                AutoCadSchemas.Props(
                    ("handle", "string", "Text entity handle"),
                    ("text", "string", "New text")),
                ["handle", "text"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var handle = reader.RequireString("handle");
                var text = reader.RequireString("text");
                var data = TransactionRunner.Run(document, (database, transaction) =>
                {
                    var entity = transaction.GetObject(AutoCadDb.ObjectIdFromHandle(database, handle), OpenMode.ForWrite);
                    switch (entity)
                    {
                        case DBText dbText:
                            dbText.TextString = text;
                            break;
                        case MText mText:
                            mText.Contents = text;
                            break;
                        default:
                            throw new ArgumentException("Entity is not DBText or MText.");
                    }

                    return new { handle, text };
                });
                return ToolResponse.Success(data, "Updated text.");
            }));

    private static System.Text.Json.JsonElement TextSchema(bool extraWidth = false)
    {
        var props = AutoCadSchemas.Props(
            ("x", "number", "Insertion X"),
            ("y", "number", "Insertion Y"),
            ("z", "number", "Insertion Z"),
            ("text", "string", "Text content"),
            ("height", "number", "Text height"),
            ("rotation", "number", "Rotation in degrees"),
            ("layer", "string", "Target layer"));
        if (extraWidth)
            props["width"] = new Dictionary<string, object> { ["type"] = "number", ["description"] = "MText width" };
        return AutoCadSchemas.Object(props, ["x", "y", "text"]);
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
