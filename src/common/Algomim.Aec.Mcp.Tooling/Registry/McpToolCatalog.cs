using System.Linq;
using Algomim.Aec.Mcp.Protocol;
using Algomim.Aec.Mcp.Tooling;

namespace Algomim.Aec.Mcp.Tooling.Registry;

/// <summary>Immutable in-memory MCP tool catalog shared by host adapters.</summary>
public sealed class McpToolCatalog : IToolCatalog
{
    private readonly IReadOnlyList<IMcpTool> _tools;
    private readonly IReadOnlyDictionary<string, IMcpTool> _byName;
    private readonly IReadOnlyList<McpToolDefinition> _definitions;

    public McpToolCatalog(IEnumerable<IMcpTool> tools)
    {
        _tools = tools.ToList();
        _byName = _tools.ToDictionary(tool => tool.Name, StringComparer.Ordinal);
        _definitions = _tools
            .Select(tool => new McpToolDefinition
            {
                Name = tool.Name,
                Description = tool.Description,
                InputSchema = tool.InputSchema,
            })
            .ToList();
    }

    public IReadOnlyList<IMcpTool> Tools => _tools;
    public IReadOnlyList<McpToolDefinition> Definitions => _definitions;
    public IMcpTool? TryGetTool(string name) => _byName.GetValueOrDefault(name);
    public int Count => _tools.Count;
}
