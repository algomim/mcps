namespace Algomim.Aec.Mcp.Protocol;

/// <summary>MCP method names and protocol version shared by all host adapters.</summary>
public static class McpConstants
{
    // JSON-RPC method names
    public const string MethodInitialize = "initialize";
    public const string MethodInitialized = "initialized";
    public const string MethodNotificationsInitialized = "notifications/initialized";
    public const string MethodToolsList = "tools/list";
    public const string MethodToolsCall = "tools/call";
    public const string MethodResourcesList = "resources/list";
    public const string MethodPing = "ping";

    // Streamable HTTP transport spec version (matches the stateless host).
    public const string ProtocolVersion = "2025-06-18";
}
