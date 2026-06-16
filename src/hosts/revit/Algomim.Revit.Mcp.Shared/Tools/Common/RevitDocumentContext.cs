using Autodesk.Revit.DB;
using RevitDocument = Autodesk.Revit.DB.Document;

namespace Algomim.Revit.Mcp.Tools.Common;

public interface IRevitDocumentContextStore
{
    ResolvedDocument Resolve(RevitDocument mainDocument);
    ResolvedDocument SwitchToMain(RevitDocument mainDocument);
    ResolvedDocument SwitchToLinked(RevitDocument mainDocument, long rawLinkInstanceId);
}

internal sealed class RevitDocumentContextStore : IRevitDocumentContextStore
{
    private long? _linkedInstanceId;

    public ResolvedDocument Resolve(RevitDocument mainDocument)
    {
        if (_linkedInstanceId is not { } rawId)
            return new ResolvedDocument(mainDocument, false, null);

        var linkInstanceId = new ElementId(rawId);
        if (mainDocument.GetElement(linkInstanceId) is not RevitLinkInstance linkInstance)
        {
            _linkedInstanceId = null;
            return new ResolvedDocument(mainDocument, false, null);
        }

        var linkedDocument = linkInstance.GetLinkDocument();
        if (linkedDocument is null)
        {
            _linkedInstanceId = null;
            return new ResolvedDocument(mainDocument, false, null);
        }

        return new ResolvedDocument(linkedDocument, true, linkInstanceId);
    }

    public ResolvedDocument SwitchToMain(RevitDocument mainDocument)
    {
        _linkedInstanceId = null;
        return new ResolvedDocument(mainDocument, false, null);
    }

    public ResolvedDocument SwitchToLinked(RevitDocument mainDocument, long rawLinkInstanceId)
    {
        var linkInstanceId = new ElementId(rawLinkInstanceId);
        if (mainDocument.GetElement(linkInstanceId) is not RevitLinkInstance linkInstance)
            throw new ToolArgumentException("linkInstanceId", "must reference a RevitLinkInstance in the active model");

        var linkedDocument = linkInstance.GetLinkDocument();
        if (linkedDocument is null)
            throw new ToolArgumentException("linkInstanceId", "linked document is not loaded");

        _linkedInstanceId = rawLinkInstanceId;
        return new ResolvedDocument(linkedDocument, true, linkInstanceId);
    }
}

public sealed record ResolvedDocument(RevitDocument Document, bool IsLinkedDocument, ElementId? LinkedInstanceId);
