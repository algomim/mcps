using Autodesk.Revit.DB;
using Algomim.Aec.Mcp.Tooling;

namespace Algomim.Revit.Mcp.Harness;

/// <summary>
/// Runs a write action inside a Revit transaction with auto-rollback and a <see cref="FailuresGuard"/>
/// so warnings never hang automation. On error or exception the transaction is rolled back and the
/// failure is returned as a structured result — it never escapes into Revit.
/// </summary>
public static class TransactionRunner
{
    public static McpToolResult Run(Document doc, string undoName, Func<McpToolResult> action)
    {
        using var transaction = new Transaction(doc, undoName);
        try
        {
            transaction.Start();

            var guard = new FailuresGuard();
            var options = transaction.GetFailureHandlingOptions();
            options.SetFailuresPreprocessor(guard);
            options.SetClearAfterRollback(true);
            transaction.SetFailureHandlingOptions(options);

            var result = action();

            if (result.IsError)
            {
                transaction.RollBack();
                return result;
            }

            var status = transaction.Commit();
            if (status != TransactionStatus.Committed)
            {
                var details = guard.ErrorMessages.Count == 0
                    ? status.ToString()
                    : string.Join("; ", guard.ErrorMessages);
                return McpToolResult.Error($"[TRANSACTION_ROLLED_BACK] {details}");
            }

            return result;
        }
        catch (Exception ex)
        {
            if (transaction.HasStarted())
                transaction.RollBack();

            return McpToolResult.Error($"[TRANSACTION_ERROR] {ex.GetType().Name}: {ex.Message}");
        }
    }
}
