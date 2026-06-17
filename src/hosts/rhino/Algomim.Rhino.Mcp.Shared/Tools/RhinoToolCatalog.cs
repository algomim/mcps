using Algomim.Aec.Mcp.Tooling;
using Algomim.Rhino.Mcp.Tools.Command;
using Algomim.Rhino.Mcp.Tools.Document;
using Algomim.Rhino.Mcp.Tools.Entity;
using Algomim.Rhino.Mcp.Tools.Geometry;
using Algomim.Rhino.Mcp.Tools.Grasshopper;
using Algomim.Rhino.Mcp.Tools.Layer;
using Algomim.Rhino.Mcp.Tools.Selection;
using Algomim.Rhino.Mcp.Tools.View;

namespace Algomim.Rhino.Mcp.Tools;

/// <summary>Rhino tool composition point.</summary>
public static class RhinoToolCatalog
{
    public static IReadOnlyList<IMcpTool> Create()
    {
        IReadOnlyList<IMcpTool> tools =
        [
            ..DocumentToolSet.Create(),
            ..LayerToolSet.Create(),
            ..GeometryToolSet.Create(),
            ..EntityToolSet.Create(),
            ..SelectionToolSet.Create(),
            ..ViewToolSet.Create(),
            ..CommandToolSet.Create(),
            ..GrasshopperToolSet.Create(),
        ];

        var actualNames = tools.Select(tool => tool.Name).ToArray();
        if (!actualNames.SequenceEqual(RhinoToolNames.InitialCatalog, StringComparer.Ordinal))
            throw new InvalidOperationException("Rhino tool catalog registration does not match RhinoToolNames.InitialCatalog.");

        return tools;
    }
}
