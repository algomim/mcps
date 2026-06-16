using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Algomim.Revit.Mcp.Scripting;

/// <summary>Invokes the compiled <c>DynamicScript.Execute</c> via reflection.</summary>
internal static class ScriptExecutor
{
    public static object? Invoke(Assembly compiledAssembly, Document doc, UIDocument uidoc, View activeView, UIApplication uiApp, RevitParams p)
    {
        var scriptType = compiledAssembly.GetType("DynamicScript")!;
        var instance = Activator.CreateInstance(scriptType)!;
        var execute = scriptType.GetMethod("Execute")!;
        return execute.Invoke(instance, new object[] { doc, uidoc, activeView, uiApp, p });
    }
}
