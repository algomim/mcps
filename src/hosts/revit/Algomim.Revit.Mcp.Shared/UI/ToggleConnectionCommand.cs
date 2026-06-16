using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Algomim.Revit.Mcp.App;

/// <summary>Ribbon command that toggles the revit-mcp server connection for this Revit instance.</summary>
[Transaction(TransactionMode.Manual)]
public sealed class ToggleConnectionCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            RevitMcpApp.Instance?.Toggle(commandData.Application);
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
