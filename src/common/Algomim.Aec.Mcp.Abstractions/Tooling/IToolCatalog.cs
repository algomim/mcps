using Algomim.Aec.Mcp.Protocol;

namespace Algomim.Aec.Mcp.Tooling;

/// <summary>
/// Read-only view over the registered MCP tools. Phase 1 exposes all tools to every caller
/// (no audience filtering — both tools are universal).
/// </summary>
public interface IToolCatalog
{
    IReadOnlyList<IMcpTool> Tools { get; }

    /// <summary>Tool definitions for <c>tools/list</c> responses.</summary>
    IReadOnlyList<McpToolDefinition> Definitions { get; }

    /// <summary>Resolves a tool by name, or null if not registered.</summary>
    IMcpTool? TryGetTool(string name);

    int Count { get; }
}
