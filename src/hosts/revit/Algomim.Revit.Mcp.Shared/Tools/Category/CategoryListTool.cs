using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Algomim.Revit.Mcp.Harness;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Category;

/// <summary>Lists categories available in the active document.</summary>
public sealed class CategoryListTool : RevitToolBase
{
    private const int MaxLimit = 500;

    public CategoryListTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore)
        : base(dispatcher, documentContextStore) { }

    public override string Name => "category_list";

    public override string Description => "List Revit categories in the active document with ids, names, and category types.";

    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new
        {
            limit = new { type = "integer", description = $"Maximum categories to return. Default 200, max {MaxLimit}." },
        },
    });

    public override ToolMetadata Metadata { get; } = new(
        "category_list",
        ToolCategory.Category,
        ToolMode.Read,
        ToolRisk.Low,
        "List Revit categories.");

    protected override McpToolResult ExecuteOnUiThread(UIApplication uiApp, ArgumentReader arguments)
    {
        var context = TryCreateContext(uiApp, out var error);
        if (context is null) return error;

        var limit = arguments.OptionalInt("limit", 200, min: 1, max: MaxLimit);
        var categories = context.Document.Settings.Categories
            .Cast<Autodesk.Revit.DB.Category>()
            .OrderBy(category => category.Name)
            .ToList();

        var data = new
        {
            count = categories.Count,
            categories = categories.Take(limit).Select(ToDto).ToList(),
            truncated = categories.Count > limit,
        };

        return Success(data, $"{categories.Count} categories found.");
    }

    private static object ToDto(Autodesk.Revit.DB.Category category)
        => new
        {
            id = category.Id.Value,
            name = category.Name,
            categoryType = category.CategoryType.ToString(),
            allowsBoundParameters = category.AllowsBoundParameters,
            hasMaterialQuantities = category.HasMaterialQuantities,
        };
}
