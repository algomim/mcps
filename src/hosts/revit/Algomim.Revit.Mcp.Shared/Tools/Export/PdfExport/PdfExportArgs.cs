using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Export.PdfExport;

internal sealed record PdfExportArgs(
    IReadOnlyList<string> FileNames,
    IReadOnlyList<long> ViewIds,
    string FolderPath,
    bool Combine)
{
    public static PdfExportArgs From(ArgumentReader arguments)
        => new(
            arguments.RequireStringArray("fileNames", 500),
            arguments.RequireLongArray("viewIds", 500),
            arguments.RequireString("folderPath"),
            arguments.RequireBool("combine"));
}
