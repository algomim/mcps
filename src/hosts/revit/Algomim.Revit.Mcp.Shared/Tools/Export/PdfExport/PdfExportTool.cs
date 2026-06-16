using System.Text.Json;
using Algomim.Aec.Mcp.Core.Operations.Export;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Composition;

namespace Algomim.Revit.Mcp.Tools.Export.PdfExport;

internal sealed class PdfExportTool : RevitPlannedWriteToolBase<PdfExportPlan>
{
    private readonly RevitPdfExportExecutor _executor;

    private PdfExportTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore, RevitPdfExportExecutor executor)
        : base(dispatcher, documentContextStore) => _executor = executor;

    public override string Name => "export_pdf";
    public override string Description => "Export views/sheets to PDF.";
    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new
        {
            fileNames = ArrayOf("string"),
            viewIds = ArrayOf("integer"),
            folderPath = new { type = "string" },
            combine = new { type = "boolean" },
        },
        required = new[] { "fileNames", "viewIds", "folderPath", "combine" },
    });
    public override ToolMetadata Metadata { get; } = new("export_pdf", ToolCategory.Export, ToolMode.Write, ToolRisk.High, "Export views/sheets to PDF.");

    public static IMcpTool Create(RevitToolServices services)
        => new PdfExportTool(services.Dispatcher, services.DocumentContextStore, new RevitPdfExportExecutor());

    protected override PdfExportPlan CreatePlan(ArgumentReader arguments)
    {
        var args = PdfExportArgs.From(arguments);
        return PdfExportPlanner.CreatePlan(args.FileNames, args.ViewIds, args.FolderPath, args.Combine);
    }

    protected override McpToolResult ExecutePlan(RevitToolContext context, PdfExportPlan plan)
        => _executor.Execute(context, plan);

    private static object ArrayOf(string type)
        => new { type = "array", items = new { type } };
}
