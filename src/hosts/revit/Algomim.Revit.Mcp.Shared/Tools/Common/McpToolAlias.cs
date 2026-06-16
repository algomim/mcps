using System.Text.Json;
using Algomim.Aec.Mcp.Tooling;

namespace Algomim.Revit.Mcp.Tools.Common;

/// <summary>Publishes an existing tool under another name without duplicating implementation.</summary>
public sealed class McpToolAlias : IMcpTool
{
    private readonly IMcpTool _inner;

    public McpToolAlias(string name, string description, IMcpTool inner)
    {
        Name = name;
        Description = description;
        _inner = inner;
    }

    public string Name { get; }
    public string Description { get; }
    public JsonElement InputSchema => _inner.InputSchema;
    public Task<McpToolResult> ExecuteAsync(JsonElement? arguments) => _inner.ExecuteAsync(arguments);
}

