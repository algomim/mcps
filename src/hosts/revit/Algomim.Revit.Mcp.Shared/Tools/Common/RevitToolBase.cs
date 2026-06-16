using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Algomim.Aec.Mcp.Core.Validation;
using Algomim.Revit.Mcp.Harness;
using Algomim.Aec.Mcp.Tooling;
using RevitDocument = Autodesk.Revit.DB.Document;
using RevitView = Autodesk.Revit.DB.View;

namespace Algomim.Revit.Mcp.Tools.Common;

/// <summary>Base class for typed Revit tools that need the Revit UI thread.</summary>
public abstract class RevitToolBase : IMcpTool, IToolMetadataProvider
{
    private readonly IUiThreadDispatcher _dispatcher;
    private readonly IRevitDocumentContextStore _documentContextStore;

    protected RevitToolBase(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore)
    {
        _dispatcher = dispatcher;
        _documentContextStore = documentContextStore;
    }

    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract JsonElement InputSchema { get; }
    public abstract ToolMetadata Metadata { get; }

    public async Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        try
        {
            var reader = new ArgumentReader(arguments);
            return await _dispatcher.InvokeOnUiThreadAsync(uiApp => ExecuteOnUiThread(uiApp, reader));
        }
        catch (ToolArgumentException ex)
        {
            return Error("INVALID_ARGUMENT", ex.Message, new { argument = ex.ArgumentName });
        }
        catch (ToolPlanValidationException ex)
        {
            return Error("INVALID_ARGUMENT", ex.Message, new { argument = ex.Field });
        }
        catch (Exception ex)
        {
            return Error("TOOL_ERROR", ex.Message, new { type = ex.GetType().Name });
        }
    }

    protected abstract McpToolResult ExecuteOnUiThread(UIApplication uiApp, ArgumentReader arguments);

    protected RevitToolContext? TryCreateContext(UIApplication uiApp, out McpToolResult error)
        => TryCreateContext(uiApp, _documentContextStore, out error);

    public static RevitToolContext? TryCreateContext(
        UIApplication uiApp,
        IRevitDocumentContextStore documentContextStore,
        out McpToolResult error)
    {
        var uidoc = uiApp.ActiveUIDocument;
        if (uidoc?.Document is not { } doc)
        {
            error = Error("NO_ACTIVE_DOCUMENT", "No active Revit document.");
            return null;
        }

        error = null!;
        var resolved = documentContextStore.Resolve(doc);
        return new RevitToolContext(uiApp, uidoc, resolved.Document, uidoc.ActiveView, doc, resolved.IsLinkedDocument, resolved.LinkedInstanceId);
    }

    protected static McpToolResult Success(object? data, string? summary = null, IReadOnlyList<string>? warnings = null)
        => McpToolResult.Success(ToolResponse.Success(data, summary, warnings));

    protected static McpToolResult Error(string code, string message, object? details = null)
        => new()
        {
            IsError = true,
            Content = { new TextContent(JsonSerializer.Serialize(ToolResponse.Failure(code, message, details), McpJson.Default)) },
        };
}

/// <summary>Common Revit objects available to typed tools after UI-thread validation.</summary>
public sealed record RevitToolContext(
    UIApplication UiApp,
    UIDocument UiDocument,
    RevitDocument Document,
    RevitView? ActiveView,
    RevitDocument MainDocument,
    bool IsLinkedDocument = false,
    ElementId? LinkedInstanceId = null);
