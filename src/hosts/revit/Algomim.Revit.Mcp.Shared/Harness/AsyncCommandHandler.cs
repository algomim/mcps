using System.Collections.Concurrent;
using Autodesk.Revit.UI;

namespace Algomim.Revit.Mcp.Harness;

/// <summary>
/// IExternalEventHandler that drains a queue of UI-thread actions when Revit raises the event.
/// The queued lambdas must be pure-synchronous Revit work — see <see cref="UiThreadDispatcher"/>.
/// </summary>
public sealed class AsyncCommandHandler : IExternalEventHandler
{
    private readonly ConcurrentQueue<Action<UIApplication>> _queue = new();

    public string GetName() => "revit-mcp UI dispatcher";

    public void EnqueueAction(Action<UIApplication> action) => _queue.Enqueue(action);

    public void Execute(UIApplication app)
    {
        while (_queue.TryDequeue(out var action))
        {
            try
            {
                action(app);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[revit-mcp] UI action error: {ex.Message}");
            }
        }
    }
}
