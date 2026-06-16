using System.Text.Json;
using Autodesk.Revit.UI;
using Algomim.Revit.Mcp.Harness;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Selection;

/// <summary>Returns the current user selection in Revit.</summary>
public sealed class SelectionGetTool : RevitToolBase
{
    private const int MaxElements = 200;

    public SelectionGetTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore)
        : base(dispatcher, documentContextStore) { }

    public override string Name => "selection_get";

    public override string Description =>
        "Get selected Revit element ids and compact element summaries for the current user selection.";

    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new
        {
            limit = new { type = "integer", description = $"Maximum selected elements to summarize. Default 100, max {MaxElements}." },
        },
    });

    public override ToolMetadata Metadata { get; } = new(
        "selection_get",
        ToolCategory.Selection,
        ToolMode.Read,
        ToolRisk.Low,
        "Get current Revit selection.");

    protected override McpToolResult ExecuteOnUiThread(UIApplication uiApp, ArgumentReader arguments)
    {
        var context = TryCreateContext(uiApp, out var error);
        if (context is null) return error;

        var limit = arguments.OptionalInt("limit", 100, min: 1, max: MaxElements);
        var ids = context.UiDocument.Selection.GetElementIds().ToList();
        var elements = ids
            .Take(limit)
            .Select(id => context.Document.GetElement(id))
            .Where(element => element is not null)
            .Select(element => RevitElementSummary.FromElement(element!))
            .ToList();

        var data = new
        {
            count = ids.Count,
            elementIds = ids.Select(id => id.Value).ToList(),
            elements,
            truncated = ids.Count > limit,
        };

        return Success(data, $"{ids.Count} element(s) selected.");
    }
}
