using System.Globalization;
using System.Text.Json;
using Rhino.Geometry;

namespace Algomim.Rhino.Mcp.Tools.Common;

internal readonly struct ArgumentReader
{
    private readonly JsonElement? _arguments;

    public ArgumentReader(JsonElement? arguments)
    {
        _arguments = arguments;
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

    public string RequireString(string name)
    {
        var value = GetString(name);
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"Argument '{name}' is required.");

        return value;
    }

    public int GetInt(string name, int fallback = 0)
    {
        if (!TryGet(name, out var value)) return fallback;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)) return number;
        if (value.ValueKind == JsonValueKind.String &&
            int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
        {
            return number;
        }

        return fallback;
    }

    public double RequireDouble(string name)
    {
        if (!TryGetDouble(name, out var value))
            throw new ArgumentException($"Argument '{name}' is required.");

        return value;
    }

    public double GetDouble(string name, double fallback = 0)
        => TryGetDouble(name, out var value) ? value : fallback;

    public bool GetBool(string name, bool fallback = false)
    {
        if (!TryGet(name, out var value)) return fallback;
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
            _ => fallback,
        };
    }

    public Guid RequireGuid(string name)
    {
        var value = RequireString(name);
        if (!Guid.TryParse(value, out var guid))
            throw new ArgumentException($"Argument '{name}' must be a GUID.");

        return guid;
    }

    public Point3d RequirePoint(string name)
    {
        if (!TryGet(name, out var value))
            throw new ArgumentException($"Argument '{name}' is required.");

        return ReadPoint(value, name);
    }

    public Point3d GetPoint(string name, Point3d fallback)
        => TryGet(name, out var value) ? ReadPoint(value, name) : fallback;

    public Vector3d RequireVector(string name)
    {
        var point = RequirePoint(name);
        return new Vector3d(point.X, point.Y, point.Z);
    }

    public Vector3d GetVector(string name, Vector3d fallback)
    {
        if (!TryGet(name, out var value))
            return fallback;

        var point = ReadPoint(value, name);
        return new Vector3d(point.X, point.Y, point.Z);
    }

    public IReadOnlyList<Point3d> RequirePointArray(string name)
    {
        if (!TryGet(name, out var value) || value.ValueKind != JsonValueKind.Array)
            throw new ArgumentException($"Argument '{name}' must be an array.");

        return value.EnumerateArray()
            .Select((item, index) => ReadPoint(item, $"{name}[{index}]"))
            .ToArray();
    }

    public IReadOnlyList<string> GetStringArray(string name)
    {
        if (!TryGet(name, out var value)) return Array.Empty<string>();
        if (value.ValueKind != JsonValueKind.Array)
            throw new ArgumentException($"Argument '{name}' must be an array.");

        return value.EnumerateArray()
            .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!)
            .ToArray();
    }

    public IReadOnlyList<double> GetDoubleArray(string name)
    {
        if (!TryGet(name, out var value)) return Array.Empty<double>();
        if (value.ValueKind != JsonValueKind.Array)
            throw new ArgumentException($"Argument '{name}' must be an array.");

        return value.EnumerateArray()
            .Select(ReadDouble)
            .ToArray();
    }

    public IReadOnlyList<double> RequireDoubleArray(string name)
    {
        var values = GetDoubleArray(name);
        if (values.Count == 0)
            throw new ArgumentException($"Argument '{name}' must contain at least one value.");

        return values;
    }

    public IReadOnlyList<string> RequireStringArray(string name)
    {
        var values = GetStringArray(name);
        if (values.Count == 0)
            throw new ArgumentException($"Argument '{name}' must contain at least one value.");

        return values;
    }

    public IReadOnlyList<Guid> RequireGuidArray(string name)
        => RequireStringArray(name)
            .Select(value =>
            {
                if (!Guid.TryParse(value, out var guid))
                    throw new ArgumentException($"Argument '{name}' contains an invalid GUID: {value}");

                return guid;
            })
            .ToArray();

    public System.Drawing.Color? GetColor(string name)
    {
        if (!TryGet(name, out var value))
            return null;

        int r;
        int g;
        int b;
        if (value.ValueKind == JsonValueKind.Array)
        {
            var values = value.EnumerateArray().Select(item => item.GetInt32()).ToArray();
            if (values.Length != 3)
                throw new ArgumentException($"Argument '{name}' must be an RGB array with 3 numbers.");

            (r, g, b) = (values[0], values[1], values[2]);
        }
        else if (value.ValueKind == JsonValueKind.Object)
        {
            r = ReadIntProperty(value, "r", name);
            g = ReadIntProperty(value, "g", name);
            b = ReadIntProperty(value, "b", name);
        }
        else
        {
            throw new ArgumentException($"Argument '{name}' must be an RGB array or object.");
        }

        if (r is < 0 or > 255 || g is < 0 or > 255 || b is < 0 or > 255)
            throw new ArgumentException($"Argument '{name}' RGB values must be between 0 and 255.");

        return System.Drawing.Color.FromArgb(r, g, b);
    }

    public JsonElement? GetElement(string name)
        => TryGet(name, out var value) ? value : null;

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

    private static Point3d ReadPoint(JsonElement value, string name)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            var coordinates = value.EnumerateArray().Select(ReadDouble).ToArray();
            if (coordinates.Length is < 2 or > 3)
                throw new ArgumentException($"Argument '{name}' must contain two or three coordinates.");

            return new Point3d(coordinates[0], coordinates[1], coordinates.Length == 3 ? coordinates[2] : 0);
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            var x = ReadDoubleProperty(value, "x", name);
            var y = ReadDoubleProperty(value, "y", name);
            var z = value.TryGetProperty("z", out var zElement) ? ReadDouble(zElement) : 0;
            return new Point3d(x, y, z);
        }

        throw new ArgumentException($"Argument '{name}' must be a point array or object.");
    }

    private static double ReadDoubleProperty(JsonElement value, string propertyName, string argumentName)
    {
        if (!value.TryGetProperty(propertyName, out var property))
            throw new ArgumentException($"Argument '{argumentName}' must include '{propertyName}'.");

        return ReadDouble(property);
    }

    private static int ReadIntProperty(JsonElement value, string propertyName, string argumentName)
    {
        if (!value.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out var number))
            throw new ArgumentException($"Argument '{argumentName}' must include numeric '{propertyName}'.");

        return number;
    }

    private static double ReadDouble(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
            return number;

        if (value.ValueKind == JsonValueKind.String &&
            double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number))
        {
            return number;
        }

        throw new ArgumentException("Coordinate values must be numbers.");
    }
}
