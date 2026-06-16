using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Composition;
using Algomim.Revit.Mcp.Tools.Export.CadExport;
using Algomim.Revit.Mcp.Tools.Export.PdfExport;

namespace Algomim.Revit.Mcp.Tools.Export;

internal static class ExportToolSet
{
    public static IEnumerable<IMcpTool> Create(RevitToolServices services)
    {
        yield return PdfExportTool.Create(services);
        yield return CadExportTool.Create(services);
    }
}
