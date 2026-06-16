using System.Text.Json;
using Autodesk.Revit.UI;
using Algomim.Revit.Mcp.Harness;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Category;

/// <summary>Searches Revit categories by localized category name.</summary>
public sealed class CategorySearchTool : RevitToolBase
{
    private const int MaxLimit = 100;

    public CategorySearchTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore)
        : base(dispatcher, documentContextStore) { }

    public override string Name => "category_search";

    public override string Description =>
        "Search Revit categories by localized category name. Use this before element_list_by_category.";

    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new
        {
            query = new { type = "string", description = "Case-insensitive text to match against category names." },
            limit = new { type = "integer", description = $"Maximum matches. Default 20, max {MaxLimit}." },
        },
        required = new[] { "query" },
    });

    public override ToolMetadata Metadata { get; } = new(
        "category_search",
        ToolCategory.Category,
        ToolMode.Read,
        ToolRisk.Low,
        "Search Revit categories.");

    protected override McpToolResult ExecuteOnUiThread(UIApplication uiApp, ArgumentReader arguments)
    {
        var context = TryCreateContext(uiApp, out var error);
        if (context is null) return error;

        var query = arguments.RequireString("query");
        var limit = arguments.OptionalInt("limit", 20, min: 1, max: MaxLimit);
        var matches = context.Document.Settings.Categories
            .Cast<Autodesk.Revit.DB.Category>()
            .Where(category => category.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderBy(category => category.Name.Length)
            .ThenBy(category => category.Name)
            .Take(limit)
            .Select(category => new
            {
                id = category.Id.Value,
                name = category.Name,
                categoryType = category.CategoryType.ToString(),
                allowsBoundParameters = category.AllowsBoundParameters,
            })
            .ToList();

        var data = new { query, count = matches.Count, categories = matches };
        return Success(data, $"{matches.Count} category match(es) for '{query}'.");
    }
}
