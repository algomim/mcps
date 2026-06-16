using Autodesk.AutoCAD.DatabaseServices;
using Algomim.Aec.Mcp.Tooling;
using Algomim.AutoCad.Mcp.Harness;
using Algomim.AutoCad.Mcp.Tools.Common;

namespace Algomim.AutoCad.Mcp.Tools.Export;

internal static class ExportToolSet
{
    public static IEnumerable<IMcpTool> Create(AutoCadToolServices services)
    {
        yield return ExportDxf(services);
    }

    private static IMcpTool ExportDxf(AutoCadToolServices services)
        => new DelegateAutoCadTool(
            "export_dxf",
            "Exports the current drawing to DXF using the AutoCAD database API.",
            AutoCadSchemas.Object(
                AutoCadSchemas.Props(
                    ("path", "string", "Target DXF path"),
                    ("precision", "number", "DXF numeric precision")),
                ["path"]),
            args => services.Dispatcher.InvokeAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var path = reader.RequireString("path");
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
                document.Database.DxfOut(path, reader.GetInt("precision", 16), DwgVersion.Current);
                return ToolResponse.Success(new { path }, "Exported DXF.");
            }));
}
