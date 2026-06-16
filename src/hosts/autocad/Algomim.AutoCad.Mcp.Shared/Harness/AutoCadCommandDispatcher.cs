using Autodesk.AutoCAD.ApplicationServices;
using Algomim.Aec.Mcp.Hosting;
using AutoCadApplication = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Algomim.AutoCad.Mcp.Harness;

/// <summary>Marshals AutoCAD API work onto AutoCAD's command context thread.</summary>
public sealed class AutoCadCommandDispatcher
{
    private readonly IMcpLogger _logger;

    public AutoCadCommandDispatcher(IMcpLogger logger)
    {
        _logger = logger;
    }

    public Task<T> InvokeAsync<T>(Func<Document, T> action)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        AutoCadApplication.DocumentManager.ExecuteInCommandContextAsync(async _ =>
        {
            await Task.Yield();

            try
            {
                var document = AutoCadApplication.DocumentManager.MdiActiveDocument;
                if (document is null)
                {
                    tcs.TrySetException(new InvalidOperationException("No active AutoCAD document."));
                    return;
                }

                using (document.LockDocument())
                {
                    tcs.TrySetResult(action(document));
                }
            }
            catch (Exception ex)
            {
                _logger.Error("AutoCAD command dispatch failed", ex);
                tcs.TrySetException(ex);
            }
        }, null);

        return tcs.Task;
    }

    public Task InvokeAsync(Action<Document> action)
        => InvokeAsync(document =>
        {
            action(document);
            return true;
        });
}
