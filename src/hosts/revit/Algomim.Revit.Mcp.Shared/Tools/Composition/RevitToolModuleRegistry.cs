using Algomim.Revit.Mcp.Tools.Analysis;
using Algomim.Revit.Mcp.Tools.Create;
using Algomim.Revit.Mcp.Tools.Document;
using Algomim.Revit.Mcp.Tools.Export;
using Algomim.Revit.Mcp.Tools.Family;
using Algomim.Revit.Mcp.Tools.Geometry;
using Algomim.Revit.Mcp.Tools.Graphics;
using Algomim.Revit.Mcp.Tools.Modify;
using Algomim.Revit.Mcp.Tools.Parameter;

namespace Algomim.Revit.Mcp.Tools.Composition;

/// <summary>Defines the stable module order for Revit MCP tools.</summary>
internal static class RevitToolModuleRegistry
{
    public static IReadOnlyList<IRevitToolModule> CreateDefaultModules()
        =>
        [
            new RevitToolModule("document", DocumentContextToolSet.Create),
            new RevitToolModule("family", FamilyToolSet.Create),
            new RevitToolModule("parameter", ParameterToolSet.Create),
            new RevitToolModule("geometry", GeometryToolSet.Create),
            new RevitToolModule("analysis", AnalysisToolSet.Create),
            new RevitToolModule("graphics", GraphicsToolSet.Create),
            new RevitToolModule("modify", ModifyToolSet.Create),
            new RevitToolModule("create", CreateToolSet.Create),
            new RevitToolModule("export", ExportToolSet.Create),
        ];
}
