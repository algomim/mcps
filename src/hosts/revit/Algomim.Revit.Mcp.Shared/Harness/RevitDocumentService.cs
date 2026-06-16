using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Algomim.Aec.Mcp.Core;

namespace Algomim.Revit.Mcp.Harness;

/// <summary>Accessor for the active Revit document, UI document, and view. Call from the UI thread.</summary>
public interface IRevitDocumentService
{
    Result<Document> GetActiveDocument();
    Result<UIDocument> GetActiveUIDocument();
    Result<View> GetActiveView();
}

/// <inheritdoc />
public sealed class RevitDocumentService : IRevitDocumentService
{
    private readonly Func<UIApplication?> _uiApp;

    /// <param name="uiApp">Supplies the current UIApplication; refreshed per call so it never goes stale.</param>
    public RevitDocumentService(Func<UIApplication?> uiApp) => _uiApp = uiApp;

    public Result<Document> GetActiveDocument()
    {
        var app = _uiApp();
        if (app is null) return Error.InvalidOperation("UIApplication not available.");
        var doc = app.ActiveUIDocument?.Document;
        return doc is null ? Error.NoActiveDocument() : doc;
    }

    public Result<UIDocument> GetActiveUIDocument()
    {
        var app = _uiApp();
        if (app is null) return Error.InvalidOperation("UIApplication not available.");
        var uiDoc = app.ActiveUIDocument;
        return uiDoc is null ? Error.NoActiveDocument() : uiDoc;
    }

    public Result<View> GetActiveView()
    {
        var app = _uiApp();
        if (app is null) return Error.InvalidOperation("UIApplication not available.");
        var view = app.ActiveUIDocument?.ActiveView;
        return view is null ? Error.NoActiveView() : view;
    }
}
