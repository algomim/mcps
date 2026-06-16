using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Export.CadExport;

internal sealed record CadExportArgs(
    IReadOnlyList<string> FileNames,
    IReadOnlyList<long> ViewIds,
    string FolderPath,
    bool TrueColors)
{
    public static CadExportArgs From(ArgumentReader arguments)
        => new(
            arguments.RequireStringArray("fileNames", 500),
            arguments.RequireLongArray("viewIds", 500),
            arguments.RequireString("folderPath"),
            arguments.RequireBool("trueColors"));
}
