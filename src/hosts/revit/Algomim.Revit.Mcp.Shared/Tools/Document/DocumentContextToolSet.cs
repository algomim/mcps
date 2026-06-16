using Autodesk.Revit.UI;
using Algomim.Revit.Mcp.Harness;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Composition;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Document;

internal static class DocumentContextToolSet
{
    public static IEnumerable<IMcpTool> Create(RevitToolServices services)
    {
        yield return new DelegateRevitTool(
            services.Dispatcher,
            services.DocumentContextStore,
            "document_switch_context",
            "Switch typed read tools to a linked document, or back to the active main document.",
            Schema.From(new
            {
                type = "object",
                properties = new
                {
                    linkInstanceId = new { type = "integer", description = "RevitLinkInstance element id to switch to." },
                    mainDocument = new { type = "boolean", description = "True to switch back to the active main document." },
                },
            }),
            new ToolMetadata("document_switch_context", ToolCategory.Document, ToolMode.Read, ToolRisk.Medium, "Switch document context."),
            (uiApp, arguments) => Execute(services.DocumentContextStore, uiApp, arguments));
    }

    private static McpToolResult Execute(IRevitDocumentContextStore documentContextStore, UIApplication uiApp, ArgumentReader arguments)
    {
        var uidoc = uiApp.ActiveUIDocument;
        if (uidoc?.Document is not { } mainDocument)
            return ToolResults.Error("NO_ACTIVE_DOCUMENT", "No active Revit document.");

        var switchToMain = arguments.OptionalBool("mainDocument", false);
        var resolved = switchToMain
            ? documentContextStore.SwitchToMain(mainDocument)
            : documentContextStore.SwitchToLinked(mainDocument, arguments.RequireLong("linkInstanceId"));

        var data = new
        {
            documentTitle = resolved.Document.Title,
            isLinkedDocument = resolved.IsLinkedDocument,
            linkedInstanceId = resolved.LinkedInstanceId?.Value,
        };

        return ToolResults.Success(data, resolved.IsLinkedDocument ? "Switched to linked document context." : "Switched to main document context.");
    }
}
