using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Modify.SheetSetRevisions;

internal sealed record SheetSetRevisionsArgs(IReadOnlyList<long> SheetIds, IReadOnlyList<long> RevisionIds, bool Assign)
{
    public static SheetSetRevisionsArgs From(ArgumentReader arguments)
        => new(
            arguments.RequireLongArray("sheetIds", 500),
            arguments.RequireLongArray("revisionIds", 500),
            arguments.RequireBool("assign"));
}
