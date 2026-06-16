using System.Text.Json.Serialization;

namespace Algomim.Aec.Mcp.Tooling;

/// <summary>Standard JSON envelope returned by typed MCP tools.</summary>
public sealed class ToolResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; init; }

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; init; }

    [JsonPropertyName("summary")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Summary { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    [JsonPropertyName("code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Code { get; init; }

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; init; }

    [JsonPropertyName("details")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Details { get; init; }

    public static ToolResponse Success(object? data, string? summary = null, IReadOnlyList<string>? warnings = null)
        => new()
        {
            Ok = true,
            Data = data,
            Summary = summary,
            Warnings = warnings ?? Array.Empty<string>(),
        };

    public static ToolResponse Failure(string code, string message, object? details = null, IReadOnlyList<string>? warnings = null)
        => new()
        {
            Ok = false,
            Code = code,
            Message = message,
            Details = details,
            Warnings = warnings ?? Array.Empty<string>(),
        };
}
