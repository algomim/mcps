using System.Text.Json;

namespace Algomim.Aec.Mcp.Tooling;

/// <summary>
/// A single MCP tool: a name, a description, a JSON-Schema for its arguments, and an executor.
/// Implementations are stateless and request/response only.
/// </summary>
public interface IMcpTool
{
    string Name { get; }
    string Description { get; }
    JsonElement InputSchema { get; }
    Task<McpToolResult> ExecuteAsync(JsonElement? arguments);
}

/// <summary>Result of a tool execution — one or more content blocks plus an error flag.</summary>
public sealed class McpToolResult
{
    public List<McpContent> Content { get; set; } = new();
    public bool IsError { get; set; }

    public static McpToolResult Success(string text)
        => new() { Content = { new TextContent(text) } };

    public static McpToolResult Success(object data)
        => Success(JsonSerializer.Serialize(data, McpJson.Default));

    public static McpToolResult SuccessWithImage(byte[] imageData, string mimeType = "image/png")
        => new() { Content = { new ImageContent(imageData, mimeType) } };

    public static McpToolResult Error(string message)
        => new() { Content = { new TextContent(message) }, IsError = true };
}
