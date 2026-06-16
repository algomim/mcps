using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Algomim.Revit.Mcp.Harness;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Element;

/// <summary>Returns compact information for specific Revit elements.</summary>
public sealed class ElementGetInfoTool : RevitToolBase
{
    private const int MaxElements = 200;

    public ElementGetInfoTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore)
        : base(dispatcher, documentContextStore) { }

    public override string Name => "element_get_info";

    public override string Description => "Get compact summaries for specific Revit element ids.";

    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new
        {
            elementIds = new { type = "array", items = new { type = "integer" }, description = $"Element ids to inspect. Max {MaxElements}." },
        },
        required = new[] { "elementIds" },
    });

    public override ToolMetadata Metadata { get; } = new(
        "element_get_info",
        ToolCategory.Element,
        ToolMode.Read,
        ToolRisk.Low,
        "Get compact element summaries.");

    protected override McpToolResult ExecuteOnUiThread(UIApplication uiApp, ArgumentReader arguments)
    {
        var context = TryCreateContext(uiApp, out var error);
        if (context is null) return error;

        var requestedIds = arguments.RequireLongArray("elementIds", MaxElements);
        var found = new List<object>();
        var missing = new List<long>();

        foreach (var rawId in requestedIds)
        {
            var element = context.Document.GetElement(new ElementId(rawId));
            if (element is null)
            {
                missing.Add(rawId);
                continue;
            }

            found.Add(RevitElementSummary.FromElement(element));
        }

        var data = new
        {
            count = found.Count,
            elements = found,
            missingElementIds = missing,
        };

        return Success(data, $"{found.Count} of {requestedIds.Count} element(s) found.");
    }
}
