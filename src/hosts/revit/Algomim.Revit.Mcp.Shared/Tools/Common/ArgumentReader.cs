using System.Text.Json;
using Algomim.Aec.Mcp.Tooling;

namespace Algomim.Revit.Mcp.Tools.Common;

/// <summary>Small, consistent reader for JSON tool arguments.</summary>
public sealed class ArgumentReader
{
    private readonly JsonElement? _arguments;

    public ArgumentReader(JsonElement? arguments) => _arguments = arguments;

    public string RequireString(string name)
    {
        var value = OptionalString(name);
        if (!string.IsNullOrWhiteSpace(value)) return value;

        throw new ToolArgumentException(name, "required string is missing or empty");
    }

    public string? OptionalString(string name)
    {
        if (!TryGet(name, out var value) || value.ValueKind != JsonValueKind.String) return null;
        return value.GetString();
    }

    public int OptionalInt(string name, int defaultValue, int? min = null, int? max = null)
    {
        if (!TryGet(name, out var value)) return defaultValue;
        if (!value.TryGetInt32(out var number))
            throw new ToolArgumentException(name, "must be an integer");

        return Clamp(name, number, min, max);
    }

    public int RequireInt(string name, int? min = null, int? max = null)
    {
        if (TryGet(name, out var value) && value.TryGetInt32(out var number))
            return Clamp(name, number, min, max);

        throw new ToolArgumentException(name, "required integer is missing");
    }

    public long RequireLong(string name)
    {
        if (TryGet(name, out var value) && value.TryGetInt64(out var number)) return number;
        throw new ToolArgumentException(name, "required integer is missing");
    }

    public long OptionalLong(string name, long defaultValue)
    {
        if (!TryGet(name, out var value)) return defaultValue;
        if (value.TryGetInt64(out var number)) return number;
        throw new ToolArgumentException(name, "must be an integer");
    }

    public double RequireDouble(string name)
    {
        if (TryGet(name, out var value) && value.TryGetDouble(out var number)) return number;
        throw new ToolArgumentException(name, "required number is missing");
    }

    public double OptionalDouble(string name, double defaultValue)
    {
        if (!TryGet(name, out var value)) return defaultValue;
        if (value.TryGetDouble(out var number)) return number;
        throw new ToolArgumentException(name, "must be a number");
    }

    public bool RequireBool(string name)
    {
        if (TryGet(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            return value.GetBoolean();

        throw new ToolArgumentException(name, "required boolean is missing");
    }

    public bool OptionalBool(string name, bool defaultValue)
    {
        if (!TryGet(name, out var value)) return defaultValue;
        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False) return value.GetBoolean();
        throw new ToolArgumentException(name, "must be a boolean");
    }

    public IReadOnlyList<long> RequireLongArray(string name, int maxCount)
    {
        if (!TryGet(name, out var value) || value.ValueKind != JsonValueKind.Array)
            throw new ToolArgumentException(name, "required integer array is missing");

        var values = new List<long>();
        foreach (var item in value.EnumerateArray())
        {
            if (!item.TryGetInt64(out var number))
                throw new ToolArgumentException(name, "must contain only integers");

            values.Add(number);
            if (values.Count > maxCount)
                throw new ToolArgumentException(name, $"must contain at most {maxCount} items");
        }

        return values;
    }

    public IReadOnlyList<long> OptionalLongArray(string name, int maxCount)
        => TryGet(name, out _) ? RequireLongArray(name, maxCount) : Array.Empty<long>();

    public IReadOnlyList<double> RequireDoubleArray(string name, int maxCount)
    {
        if (!TryGet(name, out var value) || value.ValueKind != JsonValueKind.Array)
            throw new ToolArgumentException(name, "required number array is missing");

        var values = new List<double>();
        foreach (var item in value.EnumerateArray())
        {
            if (!item.TryGetDouble(out var number))
                throw new ToolArgumentException(name, "must contain only numbers");

            values.Add(number);
            if (values.Count > maxCount)
                throw new ToolArgumentException(name, $"must contain at most {maxCount} items");
        }

        return values;
    }

    public IReadOnlyList<string> RequireStringArray(string name, int maxCount)
    {
        if (!TryGet(name, out var value) || value.ValueKind != JsonValueKind.Array)
            throw new ToolArgumentException(name, "required string array is missing");

        var values = new List<string>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
                throw new ToolArgumentException(name, "must contain only strings");

            values.Add(item.GetString() ?? string.Empty);
            if (values.Count > maxCount)
                throw new ToolArgumentException(name, $"must contain at most {maxCount} items");
        }

        return values;
    }

    public IReadOnlyList<bool> RequireBoolArray(string name, int maxCount)
    {
        if (!TryGet(name, out var value) || value.ValueKind != JsonValueKind.Array)
            throw new ToolArgumentException(name, "required boolean array is missing");

        var values = new List<bool>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new ToolArgumentException(name, "must contain only booleans");

            values.Add(item.GetBoolean());
            if (values.Count > maxCount)
                throw new ToolArgumentException(name, $"must contain at most {maxCount} items");
        }

        return values;
    }

    private bool TryGet(string name, out JsonElement value)
    {
        if (_arguments is { ValueKind: JsonValueKind.Object } args && args.TryGetProperty(name, out value))
            return true;

        value = default;
        return false;
    }

    private static int Clamp(string name, int value, int? min, int? max)
    {
        if (min.HasValue && value < min.Value)
            throw new ToolArgumentException(name, $"must be greater than or equal to {min.Value}");

        if (max.HasValue && value > max.Value)
            throw new ToolArgumentException(name, $"must be less than or equal to {max.Value}");

        return value;
    }
}

public sealed class ToolArgumentException : Exception
{
    public string ArgumentName { get; }

    public ToolArgumentException(string argumentName, string message)
        : base($"Invalid argument '{argumentName}': {message}.") => ArgumentName = argumentName;
}
