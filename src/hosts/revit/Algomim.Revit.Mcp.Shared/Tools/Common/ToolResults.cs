using System.Text.Json;
using Algomim.Aec.Mcp.Tooling;

namespace Algomim.Revit.Mcp.Tools.Common;

internal static class ToolResults
{
    public static McpToolResult Success(object? data, string? summary = null, IReadOnlyList<string>? warnings = null)
        => McpToolResult.Success(ToolResponse.Success(data, summary, warnings));

    public static McpToolResult Error(string code, string message, object? details = null)
        => new()
        {
            IsError = true,
            Content = { new TextContent(JsonSerializer.Serialize(ToolResponse.Failure(code, message, details), McpJson.Default)) },
        };
}

