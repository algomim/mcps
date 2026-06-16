using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Algomim.Revit.Mcp.Harness;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Element;

/// <summary>Lists instance elements for a document category id.</summary>
public sealed class ElementListByCategoryTool : RevitToolBase
{
    private const int MaxLimit = 1000;

    public ElementListByCategoryTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore)
        : base(dispatcher, documentContextStore) { }

    public override string Name => "element_list_by_category";

    public override string Description =>
        "List instance element ids and compact summaries for a Revit category id.";

    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new
        {
            categoryId = new { type = "integer", description = "Revit category ElementId.Value." },
            limit = new { type = "integer", description = $"Maximum elements to return. Default 200, max {MaxLimit}." },
        },
        required = new[] { "categoryId" },
    });

    public override ToolMetadata Metadata { get; } = new(
        "element_list_by_category",
        ToolCategory.Element,
        ToolMode.Read,
        ToolRisk.Low,
        "List elements by category.");

    protected override McpToolResult ExecuteOnUiThread(UIApplication uiApp, ArgumentReader arguments)
    {
        var context = TryCreateContext(uiApp, out var error);
        if (context is null) return error;

        var categoryId = new ElementId(arguments.RequireLong("categoryId"));
        var limit = arguments.OptionalInt("limit", 200, min: 1, max: MaxLimit);
        var category = context.Document.Settings.Categories
            .Cast<Autodesk.Revit.DB.Category>()
            .FirstOrDefault(candidate => candidate.Id.Value == categoryId.Value);
        if (category is null)
            return Error("CATEGORY_NOT_FOUND", $"Category id {categoryId.Value} was not found.");

        var ids = new FilteredElementCollector(context.Document)
            .OfCategoryId(categoryId)
            .WhereElementIsNotElementType()
            .ToElementIds()
            .ToList();

        var elements = ids
            .Take(limit)
            .Select(id => context.Document.GetElement(id))
            .Where(element => element is not null)
            .Select(element => RevitElementSummary.FromElement(element!))
            .ToList();

        var data = new
        {
            category = new { id = category.Id.Value, name = category.Name, categoryType = category.CategoryType.ToString() },
            count = ids.Count,
            elementIds = ids.Take(limit).Select(id => id.Value).ToList(),
            elements,
            truncated = ids.Count > limit,
        };

        return Success(data, $"{ids.Count} element(s) found in category '{category.Name}'.");
    }
}
