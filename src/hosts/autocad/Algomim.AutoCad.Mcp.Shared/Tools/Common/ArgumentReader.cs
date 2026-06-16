using System.Globalization;
using System.Text.Json;

namespace Algomim.AutoCad.Mcp.Tools.Common;

internal readonly struct ArgumentReader
{
    private readonly JsonElement? _arguments;

    public ArgumentReader(JsonElement? arguments)
    {
        _arguments = arguments;
    }

    public string RequireString(string name)
    {
        var value = GetString(name);
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"Argument '{name}' is required.");
        return value;
    }

    public string? GetString(string name, string? fallback = null)
    {
        if (!TryGet(name, out var value)) return fallback;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => fallback,
        };
    }

    public double RequireDouble(string name)
    {
        if (!TryGetDouble(name, out var value))
            throw new ArgumentException($"Argument '{name}' is required.");
        return value;
    }

    public double GetDouble(string name, double fallback = 0)
        => TryGetDouble(name, out var value) ? value : fallback;

    public int GetInt(string name, int fallback = 0)
    {
        if (!TryGet(name, out var value)) return fallback;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)) return number;
        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) return number;
        return fallback;
    }

    public bool? GetBool(string name)
    {
        if (!TryGet(name, out var value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null,
        };
    }

    public IReadOnlyList<string> RequireStringArray(string name)
    {
        if (!TryGet(name, out var value) || value.ValueKind != JsonValueKind.Array)
            throw new ArgumentException($"Argument '{name}' must be an array.");

        return value.EnumerateArray()
            .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!)
            .ToArray();
    }

    public IReadOnlyList<PointInput> RequirePointArray(string name)
    {
        if (!TryGet(name, out var value) || value.ValueKind != JsonValueKind.Array)
            throw new ArgumentException($"Argument '{name}' must be an array.");

        return value.EnumerateArray()
            .Select(item =>
            {
                if (item.ValueKind != JsonValueKind.Object)
                    throw new ArgumentException($"Argument '{name}' must contain point objects.");

                var reader = new ArgumentReader(item);
                return new PointInput(
                    reader.RequireDouble("x"),
                    reader.RequireDouble("y"),
                    reader.GetDouble("z", 0),
                    reader.GetDouble("bulge", 0));
            })
            .ToArray();
    }

    public bool Has(string name) => TryGet(name, out _);

    private bool TryGetDouble(string name, out double value)
    {
        value = 0;
        if (!TryGet(name, out var element)) return false;
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out value)) return true;
        if (element.ValueKind == JsonValueKind.String &&
            double.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }
        return false;
    }

    private bool TryGet(string name, out JsonElement value)
    {
        value = default;
        return _arguments.HasValue &&
               _arguments.Value.ValueKind == JsonValueKind.Object &&
               _arguments.Value.TryGetProperty(name, out value);
    }
}

internal readonly record struct PointInput(double X, double Y, double Z = 0, double Bulge = 0);
