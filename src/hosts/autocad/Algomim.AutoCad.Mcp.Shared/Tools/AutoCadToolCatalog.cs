using Algomim.Aec.Mcp.Tooling;
using Algomim.AutoCad.Mcp.Harness;
using Algomim.AutoCad.Mcp.Tools.Annotation;
using Algomim.AutoCad.Mcp.Tools.Block;
using Algomim.AutoCad.Mcp.Tools.Dimension;
using Algomim.AutoCad.Mcp.Tools.Document;
using Algomim.AutoCad.Mcp.Tools.Entity;
using Algomim.AutoCad.Mcp.Tools.Export;
using Algomim.AutoCad.Mcp.Tools.Geometry;
using Algomim.AutoCad.Mcp.Tools.Layer;
using Algomim.AutoCad.Mcp.Tools.Measurement;

namespace Algomim.AutoCad.Mcp.Tools;

/// <summary>AutoCAD tool composition point.</summary>
public static class AutoCadToolCatalog
{
    public static IReadOnlyList<IMcpTool> Create(AutoCadToolServices services)
    {
        IReadOnlyList<IMcpTool> tools =
        [
            ..LayerToolSet.Create(services),
            ..GeometryToolSet.Create(services),
            ..EntityToolSet.Create(services),
            ..MeasurementDrawingToolSet.Create(services),
            ..BlockToolSet.Create(services),
            ..DimensionToolSet.Create(services),
            ..AnnotationToolSet.Create(services),
            ..DocumentToolSet.Create(services),
            ..ExportToolSet.Create(services),
        ];

        var actualNames = tools.Select(tool => tool.Name).ToArray();
        if (!actualNames.SequenceEqual(AutoCadToolNames.InitialCatalog, StringComparer.Ordinal))
            throw new InvalidOperationException("AutoCAD tool catalog registration does not match AutoCadToolNames.InitialCatalog.");

        return tools;
    }
}
