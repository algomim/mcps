using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Algomim.Revit.Mcp.App;

/// <summary>Ribbon command that shows the revit-mcp HTTP endpoint for external clients.</summary>
[Transaction(TransactionMode.Manual)]
public sealed class ShowStatusCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            if (RevitMcpApp.Instance is { } app)
                app.ShowStatus();
            else
                System.Windows.Forms.MessageBox.Show(
                    "revit-mcp is not initialized.",
                    "Algomim MCP Status",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
