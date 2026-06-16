namespace Algomim.Aec.Mcp.Core.Operations.Export;

using Algomim.Aec.Mcp.Core.Commands;

public sealed record PdfExportPlan(
    IReadOnlyList<string> FileNames,
    IReadOnlyList<long> ViewIds,
    string FolderPath,
    bool Combine)
    : ToolPlan("export_pdf", ToolExecutionMode.Write);

public sealed record CadExportPlan(
    IReadOnlyList<string> FileNames,
    IReadOnlyList<long> ViewIds,
    string FolderPath,
    bool TrueColors)
    : ToolPlan("export_cad", ToolExecutionMode.Write);
