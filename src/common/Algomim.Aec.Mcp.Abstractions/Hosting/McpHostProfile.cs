namespace Algomim.Aec.Mcp.Hosting;

/// <summary>Host identity and client-facing instructions used by the MCP protocol layer.</summary>
public sealed record McpHostProfile(
    string Owner,
    string ServerName,
    string ServerInstructions);
