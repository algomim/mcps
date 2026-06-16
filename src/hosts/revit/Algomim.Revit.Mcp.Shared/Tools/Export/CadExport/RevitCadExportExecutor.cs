using System.IO;
using Autodesk.Revit.DB;
using Algomim.Aec.Mcp.Core.Operations.Export;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Export.CadExport;

internal sealed class RevitCadExportExecutor
{
    public McpToolResult Execute(RevitToolContext context, CadExportPlan plan)
    {
        Directory.CreateDirectory(plan.FolderPath);
        var viewIds = RevitIds.ToElementIds(plan.ViewIds);
        var options = new DWGExportOptions { Colors = plan.TrueColors ? ExportColorMode.TrueColorPerView : ExportColorMode.IndexColors };
        var ok = context.Document.Export(plan.FolderPath, plan.FileNames.FirstOrDefault() ?? "export", viewIds, options);
        return ToolResults.Success(new { ok, folderPath = plan.FolderPath, count = viewIds.Count }, ok ? "CAD export completed." : "CAD export returned false.");
    }
}
