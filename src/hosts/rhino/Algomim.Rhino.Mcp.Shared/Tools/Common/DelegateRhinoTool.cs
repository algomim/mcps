using System.Text.Json;
using Algomim.Aec.Mcp.Tooling;

namespace Algomim.Rhino.Mcp.Tools.Common;

internal sealed class DelegateRhinoTool : IMcpTool
{
    private readonly Func<JsonElement?, Task<ToolResponse>> _execute;

    public DelegateRhinoTool(
        string name,
        string description,
        JsonElement inputSchema,
        Func<JsonElement?, Task<ToolResponse>> execute)
    {
        Name = name;
        Description = description;
        InputSchema = inputSchema;
        _execute = execute;
    }

    public string Name { get; }
    public string Description { get; }
    public JsonElement InputSchema { get; }

    public async Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        try
        {
            var response = await _execute(arguments).ConfigureAwait(false);
            return new McpToolResult
            {
                Content = { new TextContent(JsonSerializer.Serialize(response, McpJson.Default)) },
                IsError = !response.Ok,
            };
        }
        catch (ArgumentException ex)
        {
            return Error("INVALID_ARGUMENTS", ex.Message);
        }
        catch (Exception ex)
        {
            return Error("RHINO_TOOL_FAILED", ex.Message);
        }
    }

    private static McpToolResult Error(string code, string message)
        => McpToolResult.Error(JsonSerializer.Serialize(
            ToolResponse.Failure(code, message),
            McpJson.Default));
}
