using System.Text.Json.Serialization;

namespace Algomim.Aec.Mcp.Tooling;

/// <summary>
/// Empty marker base for MCP content blocks. Runtime-type serialization is forced by
/// <see cref="McpContentJsonConverter"/> registered on the default serializer options — without it
/// System.Text.Json uses this declared base type and emits <c>{}</c> per item, which clients reject
/// as "Unexpected response type". Derived types carry their own <c>type</c> discriminator.
/// </summary>
public abstract class McpContent { }

/// <summary>Text content block.</summary>
public sealed class TextContent : McpContent
{
    [JsonPropertyName("type")]
    public string Type => "text";

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    public TextContent() { }
    public TextContent(string text) => Text = text;
}

/// <summary>Image content block (base64-encoded).</summary>
public sealed class ImageContent : McpContent
{
    [JsonPropertyName("type")]
    public string Type => "image";

    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = "image/png";

    public ImageContent() { }
    public ImageContent(byte[] imageData, string mimeType = "image/png")
    {
        Data = Convert.ToBase64String(imageData);
        MimeType = mimeType;
    }
}
