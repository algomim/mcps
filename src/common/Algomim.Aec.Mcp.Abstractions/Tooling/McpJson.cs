using System.Text.Json;
using System.Text.Json.Serialization;

namespace Algomim.Aec.Mcp.Tooling;

/// <summary>
/// Shared System.Text.Json options. Registers <see cref="McpContentJsonConverter"/> so MCP content
/// blocks serialize via their runtime type, and uses camelCase + null-omission to match the MCP wire shape.
/// </summary>
public static class McpJson
{
    public static JsonSerializerOptions Default { get; } = Build(indented: false);
    public static JsonSerializerOptions Indented { get; } = Build(indented: true);

    private static JsonSerializerOptions Build(bool indented)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = indented,
        };
        options.Converters.Add(new McpContentJsonConverter());
        return options;
    }
}
