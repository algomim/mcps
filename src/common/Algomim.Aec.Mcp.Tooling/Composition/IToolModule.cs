using Algomim.Aec.Mcp.Tooling;

namespace Algomim.Aec.Mcp.Tooling.Composition;

/// <summary>Host-neutral module contract for grouping related MCP tools behind a service object.</summary>
public interface IToolModule<in TServices>
{
    string Name { get; }

    IEnumerable<IMcpTool> CreateTools(TServices services);
}
