using Autodesk.AutoCAD.Runtime;

namespace Algomim.AutoCad.Mcp.App;

/// <summary>AutoCAD command-line entry points for the MCP plugin.</summary>
public sealed class AutoCadMcpCommands
{
    [CommandMethod("ALGOMIM_MCP_TOGGLE", CommandFlags.Session)]
    public void Toggle()
    {
        AutoCadMcpApp.Instance?.Toggle();
    }

    [CommandMethod("ALGOMIM_MCP_CONNECT", CommandFlags.Session)]
    public void Connect()
    {
        AutoCadMcpApp.Instance?.Connect();
    }

    [CommandMethod("ALGOMIM_MCP_DISCONNECT", CommandFlags.Session)]
    public void Disconnect()
    {
        AutoCadMcpApp.Instance?.Disconnect();
    }

    [CommandMethod("ALGOMIM_MCP_STATUS", CommandFlags.Session)]
    public void Status()
    {
        var status = AutoCadMcpApp.Instance?.GetStatusText() ?? "autocad-mcp is not initialized.";
        Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\n[Algomim MCP] {status}");
    }

    [CommandMethod("ALGOMIM_MCP_CHECK_UPDATE", CommandFlags.Session)]
    public void CheckUpdate()
    {
        AutoCadMcpApp.Instance?.CheckForUpdates();
    }
}
