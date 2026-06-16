using System.Text.Json;
using Algomim.Revit.Mcp.Discovery;
using Algomim.Aec.Mcp.Tooling;

namespace Algomim.Revit.Mcp.Tools;

/// <summary>
/// Introspects the live, loaded Revit API so the agent writes version-correct code. Reflection-only —
/// does not touch the UI thread.
/// </summary>
public sealed class DiscoverApiTool : IMcpTool
{
    public string Name => "discover-api";

    public string Description =>
        "Introspect the live Revit API (the running version, so results are version-correct). 'query' can be " +
        "an exact type name (e.g. 'Wall', 'CompoundStructure'), a 'Type.Member' (e.g. 'View.CropBox'), or free " +
        "text to search type names. Returns curated members/signatures. Use before script_execute when unsure about an API.";

    public JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new
        {
            query = new { type = "string", description = "Type name, 'Type.Member', or free-text search term." },
        },
        required = new[] { "query" },
    });

    public Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var query = arguments is { } args && args.TryGetProperty("query", out var q) ? q.GetString() : null;
        var result = ReflectionApiDiscovery.Discover(query ?? string.Empty);
        return Task.FromResult(McpToolResult.Success(result));
    }
}
