using System.Text.Json;
using Algomim.Aec.Mcp.Tooling;

namespace Algomim.AutoCad.Mcp.Tools.Common;

internal sealed class DelegateAutoCadTool : IMcpTool
{
    private readonly Func<JsonElement?, Task<ToolResponse>> _execute;

    public DelegateAutoCadTool(
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
            var response = await _execute(arguments);
            return new McpToolResult
            {
                Content = { new TextContent(JsonSerializer.Serialize(response, McpJson.Default)) },
                IsError = !response.Ok,
            };
        }
        catch (ArgumentException ex)
        {
            return McpToolResult.Error(JsonSerializer.Serialize(
                ToolResponse.Failure("INVALID_ARGUMENTS", ex.Message),
                McpJson.Default));
        }
        catch (Exception ex)
        {
            return McpToolResult.Error(JsonSerializer.Serialize(
                ToolResponse.Failure("AUTOCAD_TOOL_FAILED", ex.Message),
                McpJson.Default));
        }
    }
}
