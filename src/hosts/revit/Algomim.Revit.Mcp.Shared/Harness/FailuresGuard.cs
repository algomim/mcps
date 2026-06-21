using Autodesk.Revit.DB;

namespace Algomim.Revit.Mcp.Harness;

/// <summary>
/// Keeps transaction failure handling unattended without interfering with Revit startup or
/// document-open dialogs.
/// </summary>
public sealed class FailuresGuard : IFailuresPreprocessor
{
    private readonly List<string> _errorMessages = [];

    public IReadOnlyList<string> ErrorMessages => _errorMessages;

    /// <summary>Deletes warnings. Errors roll the transaction back instead of showing modal UI.</summary>
    public FailureProcessingResult PreprocessFailures(FailuresAccessor accessor)
    {
        var deletedWarnings = false;
        var sawError = false;

        foreach (var message in accessor.GetFailureMessages())
        {
            var severity = message.GetSeverity();
            if (severity == FailureSeverity.Warning)
            {
                accessor.DeleteWarning(message);
                deletedWarnings = true;
            }
            else if (severity == FailureSeverity.Error)
            {
                sawError = true;
                var description = message.GetDescriptionText();
                if (!string.IsNullOrWhiteSpace(description))
                    _errorMessages.Add(description);
            }
        }

        if (sawError)
            return FailureProcessingResult.ProceedWithRollBack;

        return deletedWarnings ? FailureProcessingResult.ProceedWithCommit : FailureProcessingResult.Continue;
    }
}
