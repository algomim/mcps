using System.Text.Json;
using System.Text.Json.Serialization;

namespace Algomim.Aec.Mcp.Tooling;

/// <summary>
/// Forces System.Text.Json to serialize <see cref="McpContent"/> instances by their runtime type
/// (<see cref="TextContent"/> / <see cref="ImageContent"/>) so their concrete properties are written.
/// Without this, STJ uses the declared (empty) base type and emits <c>{}</c>, which MCP clients
/// reject as "Unexpected response type". Read is unsupported — the plugin only produces content.
/// </summary>
public sealed class McpContentJsonConverter : JsonConverter<McpContent>
{
    // Claim ONLY the exact base type. Claiming derived types would recurse into ourselves when
    // Write re-serializes with the same options, blowing the stack and dropping the response.
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(McpContent);

    public override McpContent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException("McpContent deserialization is not supported.");

    public override void Write(Utf8JsonWriter writer, McpContent value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
