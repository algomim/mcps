namespace Algomim.Aec.Mcp.Protocol;

/// <summary>An MCP/JSON-RPC failure carrying a JSON-RPC error code (e.g. -32601 method not found).</summary>
public sealed class McpException : Exception
{
    public int Code { get; }

    public McpException(int code, string message) : base(message) => Code = code;

    public static McpException MethodNotFound(string method) => new(-32601, $"Method not found: {method}");
    public static McpException InvalidParams(string message) => new(-32602, message);
}
