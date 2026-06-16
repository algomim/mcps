using System.IO;
using Autodesk.Revit.DB;
using Algomim.Aec.Mcp.Core.Operations.Export;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Export.PdfExport;

internal sealed class RevitPdfExportExecutor
{
    public McpToolResult Execute(RevitToolContext context, PdfExportPlan plan)
    {
        Directory.CreateDirectory(plan.FolderPath);
        var viewIds = RevitIds.ToElementIds(plan.ViewIds);
        var options = new PDFExportOptions { Combine = plan.Combine };
        if (plan.FileNames.Count > 0)
            options.FileName = plan.FileNames[0];

        var ok = context.Document.Export(plan.FolderPath, viewIds, options);
        return ToolResults.Success(new { ok, folderPath = plan.FolderPath, count = viewIds.Count }, ok ? "PDF export completed." : "PDF export returned false.");
    }
}
