using Autodesk.Revit.UI;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;

namespace Algomim.Revit.Mcp.Tools.Common;

/// <summary>Base class for Revit tools that mutate the active document inside a transaction.</summary>
public abstract class RevitWriteToolBase : RevitToolBase
{
    protected RevitWriteToolBase(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore)
        : base(dispatcher, documentContextStore)
    {
    }

    protected sealed override McpToolResult ExecuteOnUiThread(UIApplication uiApp, ArgumentReader arguments)
    {
        var context = TryCreateContext(uiApp, out var error);
        if (context is null)
            return error;

        if (context.IsLinkedDocument)
            return Error("LINKED_DOCUMENT_WRITE_BLOCKED", "Cannot write to linked document context.");

        return TransactionRunner.Run(context.Document, $"revit-mcp {Name}", () => ExecuteWrite(context, arguments));
    }

    protected abstract McpToolResult ExecuteWrite(RevitToolContext context, ArgumentReader arguments);
}
