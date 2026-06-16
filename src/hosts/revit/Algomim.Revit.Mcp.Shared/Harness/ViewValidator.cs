using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Algomim.Revit.Mcp.Harness;

/// <summary>Validates that the active view can run scripts, and recovers from non-graphical views.</summary>
public static class ViewValidator
{
    /// <summary>Returns an error message if the view is non-graphical, or null if it is suitable.</summary>
    public static string? ValidateForScriptExecution(View view)
    {
        var viewType = view.ViewType;
        if (viewType is ViewType.ProjectBrowser or ViewType.SystemBrowser or ViewType.Undefined)
        {
            return $"Active view is '{viewType}', which is not a graphical view. " +
                   "Switch to a floor plan, 3D view, section, or other graphical view before running scripts.";
        }

        return null;
    }

    /// <summary>
    /// Tries to recover from a non-graphical active view by switching to the first graphical view.
    /// Returns the new view on success, or null if none was found / the switch failed.
    /// </summary>
    public static View? TryAutoRecover(Document doc, UIDocument uidoc)
    {
        var graphicalView = new FilteredElementCollector(doc)
            .OfClass(typeof(View))
            .Cast<View>()
            .FirstOrDefault(v => !v.IsTemplate &&
                v.ViewType is not (ViewType.ProjectBrowser or ViewType.SystemBrowser or
                    ViewType.Undefined or ViewType.Internal or ViewType.DrawingSheet));

        if (graphicalView is null) return null;

        try
        {
            uidoc.ActiveView = graphicalView;
            return graphicalView;
        }
        catch
        {
            return null;
        }
    }
}
