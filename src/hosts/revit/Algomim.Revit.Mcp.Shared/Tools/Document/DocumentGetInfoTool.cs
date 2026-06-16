using System.Text.Json;
using Autodesk.Revit.UI;
using Algomim.Revit.Mcp.Harness;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Document;

/// <summary>Returns basic Revit application, document, active view, and selection context.</summary>
public sealed class DocumentGetInfoTool : RevitToolBase
{
    public DocumentGetInfoTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore)
        : base(dispatcher, documentContextStore) { }

    public override string Name => "document_get_info";

    public override string Description =>
        "Get the active Revit document context: Revit version, document title/path, active view, and selection count.";

    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new { },
    });

    public override ToolMetadata Metadata { get; } = new(
        "document_get_info",
        ToolCategory.Document,
        ToolMode.Read,
        ToolRisk.Low,
        "Get active Revit document context.");

    protected override McpToolResult ExecuteOnUiThread(UIApplication uiApp, ArgumentReader arguments)
    {
        var context = TryCreateContext(uiApp, out var error);
        if (context is null) return error;

        var app = uiApp.Application;
        var doc = context.Document;
        var view = context.ActiveView;
        var selectionIds = context.UiDocument.Selection.GetElementIds();

        var data = new
        {
            revit = new
            {
                versionNumber = app.VersionNumber,
                versionName = app.VersionName,
            },
            document = new
            {
                title = doc.Title,
                path = doc.PathName,
                isFamilyDocument = doc.IsFamilyDocument,
                isWorkshared = doc.IsWorkshared,
                project = doc.ProjectInformation is null ? null : new
                {
                    id = doc.ProjectInformation.Id.Value,
                    name = doc.ProjectInformation.Name,
                    number = doc.ProjectInformation.Number,
                },
            },
            activeView = view is null ? null : new
            {
                id = view.Id.Value,
                name = view.Name,
                viewType = view.ViewType.ToString(),
                isTemplate = view.IsTemplate,
                scale = view.Scale,
            },
            selection = new
            {
                count = selectionIds.Count,
                elementIds = selectionIds.Select(id => id.Value).Take(100).ToList(),
            },
        };

        return Success(data, $"Document '{doc.Title}' is active.");
    }
}
