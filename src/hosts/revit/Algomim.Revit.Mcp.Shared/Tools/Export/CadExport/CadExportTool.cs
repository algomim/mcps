using System.Text.Json;
using Algomim.Aec.Mcp.Core.Operations.Export;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Composition;

namespace Algomim.Revit.Mcp.Tools.Export.CadExport;

internal sealed class CadExportTool : RevitPlannedWriteToolBase<CadExportPlan>
{
    private readonly RevitCadExportExecutor _executor;

    private CadExportTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore, RevitCadExportExecutor executor)
        : base(dispatcher, documentContextStore) => _executor = executor;

    public override string Name => "export_cad";
    public override string Description => "Export views/sheets to DWG.";
    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new
        {
            fileNames = ArrayOf("string"),
            viewIds = ArrayOf("integer"),
            folderPath = new { type = "string" },
            trueColors = new { type = "boolean" },
        },
        required = new[] { "fileNames", "viewIds", "folderPath", "trueColors" },
    });
    public override ToolMetadata Metadata { get; } = new("export_cad", ToolCategory.Export, ToolMode.Write, ToolRisk.High, "Export views/sheets to DWG.");

    public static IMcpTool Create(RevitToolServices services)
        => new CadExportTool(services.Dispatcher, services.DocumentContextStore, new RevitCadExportExecutor());

    protected override CadExportPlan CreatePlan(ArgumentReader arguments)
    {
        var args = CadExportArgs.From(arguments);
        return CadExportPlanner.CreatePlan(args.FileNames, args.ViewIds, args.FolderPath, args.TrueColors);
    }

    protected override McpToolResult ExecutePlan(RevitToolContext context, CadExportPlan plan)
        => _executor.Execute(context, plan);

    private static object ArrayOf(string type)
        => new { type = "array", items = new { type } };
}
