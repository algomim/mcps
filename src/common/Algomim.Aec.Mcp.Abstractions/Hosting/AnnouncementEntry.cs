using System.Text.Json.Serialization;

namespace Algomim.Aec.Mcp.Hosting;

/// <summary>
/// One connected host instance's live MCP announcement. Each instance writes its own file so
/// multiple host processes can appear and disappear independently.
/// </summary>
public sealed class AnnouncementEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("owner")]
    public string Owner { get; set; } = "revit";

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("pid")]
    public int Pid { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("document")]
    public string? Document { get; set; }

    [JsonPropertyName("port")]
    public int Port { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("healthUrl")]
    public string HealthUrl { get; set; } = string.Empty;

    [JsonPropertyName("startedAt")]
    public DateTimeOffset StartedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; }
}
