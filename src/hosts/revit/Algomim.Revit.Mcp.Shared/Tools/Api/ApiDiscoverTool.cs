using System.Text.Json;
using Algomim.Revit.Mcp.Discovery;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Api;

/// <summary>Standard named API discovery tool for version-correct Revit API reflection.</summary>
public sealed class ApiDiscoverTool : IMcpTool, IToolMetadataProvider
{
    public string Name => "api_discover";

    public string Description =>
        "Discover live Revit API types and members for the running Revit version. Use before scripts or " +
        "advanced tool composition when unsure about signatures.";

    public JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new
        {
            query = new { type = "string", description = "Type name, 'Type.Member', or free-text search term." },
        },
        required = new[] { "query" },
    });

    public ToolMetadata Metadata { get; } = new(
        "api_discover",
        ToolCategory.Api,
        ToolMode.Read,
        ToolRisk.Low,
        "Discover live Revit API types and members.");

    public Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        try
        {
            var query = new ArgumentReader(arguments).RequireString("query");
            var result = ReflectionApiDiscovery.Discover(query);
            return Task.FromResult(McpToolResult.Success(ToolResponse.Success(result, $"API discovery results for '{query}'.")));
        }
        catch (ToolArgumentException ex)
        {
            var response = ToolResponse.Failure("INVALID_ARGUMENT", ex.Message, new { argument = ex.ArgumentName });
            return Task.FromResult(new McpToolResult
            {
                IsError = true,
                Content = { new TextContent(JsonSerializer.Serialize(response, McpJson.Default)) },
            });
        }
    }
}

