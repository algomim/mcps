using System.Linq;
using System.Text.Json;

namespace Algomim.Revit.Mcp.Scripting;

/// <summary>
/// Type-safe accessor for runtime parameters passed to a compiled script as <c>p</c>. Keeping values
/// out of the code string lets identical scripts share a compile-cache entry.
/// </summary>
public sealed class RevitParams
{
    private readonly JsonElement _data;

    public RevitParams(JsonElement data) => _data = data;

    public string GetString(string key) =>
        _data.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()!
            : throw new InvalidOperationException($"Required string param '{key}' is missing or not a string.");

    public double GetDouble(string key) =>
        _data.TryGetProperty(key, out var el) && el.TryGetDouble(out var v)
            ? v
            : throw new InvalidOperationException($"Required double param '{key}' is missing or not a number.");

    public int GetInt(string key) =>
        _data.TryGetProperty(key, out var el) && el.TryGetInt32(out var v)
            ? v
            : throw new InvalidOperationException($"Required int param '{key}' is missing or not an integer.");

    public long GetLong(string key) =>
        _data.TryGetProperty(key, out var el) && el.TryGetInt64(out var v)
            ? v
            : throw new InvalidOperationException($"Required long param '{key}' is missing or not an integer.");

    public bool GetBool(string key) =>
        _data.TryGetProperty(key, out var el) && el.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? el.GetBoolean()
            : throw new InvalidOperationException($"Required bool param '{key}' is missing or not a boolean.");

    public string? TryGetString(string key) =>
        _data.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

    public double? TryGetDouble(string key) =>
        _data.TryGetProperty(key, out var el) && el.TryGetDouble(out var v) ? v : null;

    public int? TryGetInt(string key) =>
        _data.TryGetProperty(key, out var el) && el.TryGetInt32(out var v) ? v : null;

    public bool? TryGetBool(string key) =>
        _data.TryGetProperty(key, out var el) && el.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? el.GetBoolean()
            : null;

    public long[] GetLongArray(string key) =>
        _data.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.Array
            ? el.EnumerateArray().Select(e => e.GetInt64()).ToArray()
            : throw new InvalidOperationException($"Required long[] param '{key}' is missing or not an array.");

    public string[] GetStringArray(string key) =>
        _data.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.Array
            ? el.EnumerateArray().Select(e => e.GetString()!).ToArray()
            : throw new InvalidOperationException($"Required string[] param '{key}' is missing or not an array.");
}
