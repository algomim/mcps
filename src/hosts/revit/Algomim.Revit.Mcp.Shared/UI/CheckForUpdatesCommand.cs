using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Algomim.Revit.Mcp.App;

/// <summary>Ribbon command that checks GitHub Releases for a newer revit-mcp MSI.</summary>
[Transaction(TransactionMode.Manual)]
public sealed class CheckForUpdatesCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            RevitMcpApp.Instance?.CheckForUpdates();
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
