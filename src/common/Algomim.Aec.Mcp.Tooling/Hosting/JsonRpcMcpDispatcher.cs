using System.Linq;
using System.Text.Json;
using Algomim.Aec.Mcp.Hosting;
using Algomim.Aec.Mcp.Protocol;
using Algomim.Aec.Mcp.Tooling;

namespace Algomim.Aec.Mcp.Tooling.Hosting;

/// <summary>Transport-neutral JSON-RPC router for MCP methods.</summary>
public sealed class JsonRpcMcpDispatcher
{
    private readonly IToolCatalog _catalog;
    private readonly McpHostProfile _profile;
    private readonly string _version;

    public JsonRpcMcpDispatcher(IToolCatalog catalog, McpHostProfile profile, string version)
    {
        _catalog = catalog;
        _profile = profile;
        _version = version;
    }

    public async Task<object?> DispatchAsync(string? method, JsonElement parameters)
    {
        switch (method)
        {
            case McpConstants.MethodInitialize:
                return new InitializeResult
                {
                    ServerInfo = new ServerInfo { Name = _profile.ServerName, Version = _version },
                    Instructions = _profile.ServerInstructions,
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
