using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

namespace Algomim.Revit.Mcp.Harness;

/// <summary>
/// Keeps automation unattended: auto-resolves recoverable warnings inside transactions
/// (<see cref="IFailuresPreprocessor"/>) and auto-dismisses modal dialogs that would otherwise
/// block the headless UI thread (<see cref="DialogBoxShowing"/>).
/// </summary>
public sealed class FailuresGuard : IFailuresPreprocessor
{
    /// <summary>Deletes warnings and resolves resolvable errors so the transaction can commit.</summary>
    public FailureProcessingResult PreprocessFailures(FailuresAccessor accessor)
    {
        var handled = false;

        foreach (var message in accessor.GetFailureMessages())
        {
            var severity = message.GetSeverity();
            if (severity == FailureSeverity.Warning)
            {
                accessor.DeleteWarning(message);
                handled = true;
            }
            else if (severity == FailureSeverity.Error && message.HasResolutions())
            {
                accessor.ResolveFailure(message);
                handled = true;
            }
        }

        return handled ? FailureProcessingResult.ProceedWithCommit : FailureProcessingResult.Continue;
    }

    /// <summary>
    /// Auto-dismisses modal dialogs/task dialogs during automation. Subscribe in OnStartup:
    /// <c>uiCtrlApp.DialogBoxShowing += FailuresGuard.OnDialogBoxShowing;</c> and unsubscribe on shutdown.
    /// </summary>
    public static void OnDialogBoxShowing(object? sender, DialogBoxShowingEventArgs e)
    {
        // TaskDialog: confirm with the first command-link / OK so nothing hangs.
        if (e is TaskDialogShowingEventArgs task)
        {
            task.OverrideResult((int)TaskDialogResult.Ok);
            return;
        }

        // Other modal message boxes: answer OK (1).
        e.OverrideResult(1);
    }
}
