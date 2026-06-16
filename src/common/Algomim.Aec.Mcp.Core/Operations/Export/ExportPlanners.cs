namespace Algomim.Aec.Mcp.Core.Operations.Export;

using Algomim.Aec.Mcp.Core.Validation;
using static Algomim.Aec.Mcp.Core.Operations.PlanValidation;

public static class PdfExportPlanner
{
    public static PdfExportPlan CreatePlan(
        IReadOnlyList<string> fileNames,
        IReadOnlyList<long> viewIds,
        string folderPath,
        bool combine)
    {
        ExportPlanGuards.EnsureExportInputs(fileNames, viewIds, folderPath);
        return new PdfExportPlan(fileNames, viewIds, folderPath, combine);
    }
}

public static class CadExportPlanner
{
    public static CadExportPlan CreatePlan(
        IReadOnlyList<string> fileNames,
        IReadOnlyList<long> viewIds,
        string folderPath,
        bool trueColors)
    {
        ExportPlanGuards.EnsureExportInputs(fileNames, viewIds, folderPath);
        return new CadExportPlan(fileNames, viewIds, folderPath, trueColors);
    }
}

internal static class ExportPlanGuards
{
    public static void EnsureExportInputs(IReadOnlyList<string> fileNames, IReadOnlyList<long> viewIds, string folderPath)
    {
        EnsureNotEmpty(nameof(viewIds), viewIds.Count);
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ToolPlanValidationException(nameof(folderPath), "required string is missing or empty");

        if (fileNames.Count > 0)
            EnsureCompatibleLength(nameof(fileNames), viewIds.Count, fileNames.Count);
    }
}
