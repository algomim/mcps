using System.Linq;
using System.Text.Json;
using Algomim.Aec.Mcp.Protocol;
using Algomim.Aec.Mcp.Tooling;

namespace Algomim.Revit.Mcp.Hosting;

/// <summary>
/// Transport-agnostic JSON-RPC method router for MCP. Resolves tools via <see cref="IToolCatalog"/>;
/// the tools handle their own UI-thread marshalling, so the dispatcher itself never touches Revit.
/// </summary>
public sealed class McpRequestDispatcher
{
    private readonly IToolCatalog _catalog;
    private readonly string _version;

    public McpRequestDispatcher(IToolCatalog catalog, string version)
    {
        _catalog = catalog;
        _version = version;
    }

    public async Task<object?> DispatchAsync(string? method, JsonElement parameters)
    {
        switch (method)
        {
            case McpConstants.MethodInitialize:
                return new InitializeResult
                {
                    ServerInfo = new ServerInfo { Name = RevitMcpServerProfile.ServerName, Version = _version },
                    Instructions = RevitMcpServerProfile.ServerInstructions,
                };

            case McpConstants.MethodInitialized:
            case McpConstants.MethodNotificationsInitialized:
            case McpConstants.MethodPing:
                return new { };

            case McpConstants.MethodToolsList:
                return new ToolsListResult { Tools = _catalog.Definitions.ToList() };

            case McpConstants.MethodToolsCall:
                return await CallToolAsync(parameters);

            case McpConstants.MethodResourcesList:
                return new { resources = Array.Empty<object>() };

            default:
                throw McpException.MethodNotFound(method ?? "(null)");
        }
    }

    private async Task<CallToolResult> CallToolAsync(JsonElement parameters)
    {
        var name = parameters.TryGetProperty("name", out var n) ? n.GetString() : null;
        if (string.IsNullOrEmpty(name))
            throw McpException.InvalidParams("Missing tool name.");

        var tool = _catalog.TryGetTool(name);
        if (tool is null)
            throw McpException.InvalidParams($"Unknown tool: {name}");

        var arguments = parameters.TryGetProperty("arguments", out var a) ? a : (JsonElement?)null;
        var result = await tool.ExecuteAsync(arguments);

        return new CallToolResult { Content = result.Content, IsError = result.IsError };
    }
}
