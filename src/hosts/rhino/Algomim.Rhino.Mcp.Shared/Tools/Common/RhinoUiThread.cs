using Algomim.Aec.Mcp.Tooling;
using Rhino;

namespace Algomim.Rhino.Mcp.Tools.Common;

internal static class RhinoUiThread
{
    public static Task<ToolResponse> InvokeWithActiveDocumentAsync(Func<RhinoDoc, ToolResponse> execute)
        => InvokeAsync(() =>
        {
            var document = RhinoDoc.ActiveDoc;
            if (document is null)
                return ToolResponse.Failure("RHINO_DOCUMENT_NOT_AVAILABLE", "No active Rhino document is available.");

            return execute(document);
        });

    public static Task<McpToolResult> InvokeWithActiveDocumentResultAsync(Func<RhinoDoc, McpToolResult> execute)
        => InvokeAsync(() =>
        {
            var document = RhinoDoc.ActiveDoc;
            if (document is null)
                return McpToolResult.Error(System.Text.Json.JsonSerializer.Serialize(
                    ToolResponse.Failure("RHINO_DOCUMENT_NOT_AVAILABLE", "No active Rhino document is available."),
                    McpJson.Default));

            return execute(document);
        });

    public static Task<T> InvokeAsync<T>(Func<T> execute)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        RhinoApp.InvokeOnUiThread((Action)(() =>
        {
            try
            {
                completion.SetResult(execute());
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        }));

        return completion.Task;
    }
}
