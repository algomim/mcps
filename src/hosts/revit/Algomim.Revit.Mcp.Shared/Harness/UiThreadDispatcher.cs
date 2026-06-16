using Autodesk.Revit.UI;

namespace Algomim.Revit.Mcp.Harness;

/// <summary>
/// Dispatches synchronous Revit work onto the UI thread via an ExternalEvent and awaits the result.
/// The caller's lambda runs on the UI thread and must be pure-synchronous (the Revit API is
/// synchronous) — do not <c>await</c> inside it, or it will deadlock against the UI thread.
/// </summary>
public sealed class UiThreadDispatcher : IUiThreadDispatcher, IDisposable
{
    private readonly ExternalEvent _externalEvent;
    private readonly AsyncCommandHandler _handler;
    private bool _disposed;

    public UiThreadDispatcher()
    {
        _handler = new AsyncCommandHandler();
        _externalEvent = ExternalEvent.Create(_handler);
    }

    public Task<T> InvokeOnUiThreadAsync<T>(Func<UIApplication, T> action)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        _handler.EnqueueAction(uiApp =>
        {
            try
            {
                tcs.TrySetResult(action(uiApp));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        _externalEvent.Raise();
        return tcs.Task;
    }

    public Task InvokeOnUiThreadAsync(Action<UIApplication> action)
        => InvokeOnUiThreadAsync<bool>(uiApp => { action(uiApp); return true; });

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _externalEvent.Dispose();
    }
}
