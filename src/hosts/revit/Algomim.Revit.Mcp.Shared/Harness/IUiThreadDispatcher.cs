using Autodesk.Revit.UI;

namespace Algomim.Revit.Mcp.Harness;

/// <summary>
/// Marshals work onto Revit's UI thread via an ExternalEvent. All Revit API access must go through
/// here — calling the API off the UI thread crashes Revit.
/// </summary>
public interface IUiThreadDispatcher
{
    Task<T> InvokeOnUiThreadAsync<T>(Func<UIApplication, T> action);
    Task InvokeOnUiThreadAsync(Action<UIApplication> action);
}
