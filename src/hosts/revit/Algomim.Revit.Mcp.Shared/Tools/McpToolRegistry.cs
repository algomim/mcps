using System.Linq;
using Algomim.Aec.Mcp.Protocol;
using Algomim.Aec.Mcp.Tooling;

namespace Algomim.Revit.Mcp.Tools;

/// <summary>In-memory catalog of the registered MCP tools, built once at startup.</summary>
public sealed class McpToolRegistry : IToolCatalog
{
    private readonly List<IMcpTool> _tools;
    private readonly Dictionary<string, IMcpTool> _byName;
    private readonly List<McpToolDefinition> _definitions;

    public McpToolRegistry(IEnumerable<IMcpTool> tools)
    {
        _tools = tools.ToList();
        _byName = _tools.ToDictionary(t => t.Name, StringComparer.Ordinal);
        _definitions = _tools
            .Select(t => new McpToolDefinition { Name = t.Name, Description = t.Description, InputSchema = t.InputSchema })
            .ToList();
    }

    public IReadOnlyList<IMcpTool> Tools => _tools;
    public IReadOnlyList<McpToolDefinition> Definitions => _definitions;
    public IMcpTool? TryGetTool(string name) => _byName.GetValueOrDefault(name);
    public int Count => _tools.Count;
}
