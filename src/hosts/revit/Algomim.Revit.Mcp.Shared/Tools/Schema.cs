using System.Text.Json;

namespace Algomim.Revit.Mcp.Tools;

/// <summary>Builds a self-contained JSON-Schema <see cref="JsonElement"/> from an anonymous shape.</summary>
internal static class Schema
{
    public static JsonElement From(object shape)
        => JsonDocument.Parse(JsonSerializer.Serialize(shape)).RootElement.Clone();
}
