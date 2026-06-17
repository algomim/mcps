using System.Text.Json;
using Algomim.Aec.Mcp.Tooling;

namespace Algomim.Rhino.Mcp.Tools.Common;

internal static class RhinoSchemas
{
    public static JsonElement Empty { get; } = Object();

    public static JsonElement Object(
        IReadOnlyDictionary<string, object>? properties = null,
        IReadOnlyList<string>? required = null)
    {
        var schema = new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = properties ?? new Dictionary<string, object>(),
            ["additionalProperties"] = false,
        };

        if (required is { Count: > 0 })
            schema["required"] = required;

        return JsonSerializer.SerializeToElement(schema, McpJson.Default);
    }

    public static Dictionary<string, object> Props(params (string Name, string Type, string Description)[] values)
        => values.ToDictionary(
            value => value.Name,
            value => (object)new Dictionary<string, object>
            {
                ["type"] = value.Type,
                ["description"] = value.Description,
            },
            StringComparer.Ordinal);

    public static object StringArray(string description)
        => new Dictionary<string, object>
        {
            ["type"] = "array",
            ["description"] = description,
            ["items"] = new Dictionary<string, object> { ["type"] = "string" },
        };

    public static object NumberArray(string description)
        => new Dictionary<string, object>
        {
            ["type"] = "array",
            ["description"] = description,
            ["items"] = new Dictionary<string, object> { ["type"] = "number" },
        };

    public static object JsonObject(string description)
        => new Dictionary<string, object>
        {
            ["type"] = "object",
            ["description"] = description,
            ["additionalProperties"] = true,
        };

    public static object JsonObjectArray(string description)
        => new Dictionary<string, object>
        {
            ["type"] = "array",
            ["description"] = description,
            ["items"] = JsonObject("Array item"),
        };

    public static object Integer(string description, int minimum, int maximum, int defaultValue)
        => new Dictionary<string, object>
        {
            ["type"] = "integer",
            ["description"] = description,
            ["minimum"] = minimum,
            ["maximum"] = maximum,
            ["default"] = defaultValue,
        };

    public static object Number(string description, double? minimum = null, double? defaultValue = null)
    {
        var schema = new Dictionary<string, object>
        {
            ["type"] = "number",
            ["description"] = description,
        };
        if (minimum.HasValue) schema["exclusiveMinimum"] = minimum.Value;
        if (defaultValue.HasValue) schema["default"] = defaultValue.Value;
        return schema;
    }

    public static object Boolean(string description, bool defaultValue = false)
        => new Dictionary<string, object>
        {
            ["type"] = "boolean",
            ["description"] = description,
            ["default"] = defaultValue,
        };

    public static object Point(string description)
        => new Dictionary<string, object>
        {
            ["description"] = description,
            ["oneOf"] = new object[]
            {
                new Dictionary<string, object>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object> { ["type"] = "number" },
                    ["minItems"] = 2,
                    ["maxItems"] = 3,
                },
                new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = Props(
                        ("x", "number", "X coordinate"),
                        ("y", "number", "Y coordinate"),
                        ("z", "number", "Z coordinate")),
                    ["required"] = new[] { "x", "y" },
                    ["additionalProperties"] = false,
                },
            },
        };

    public static object PointArray(string description)
        => new Dictionary<string, object>
        {
            ["type"] = "array",
            ["description"] = description,
            ["items"] = Point("Point"),
        };

    public static object Color(string description)
        => new Dictionary<string, object>
        {
            ["description"] = description,
            ["oneOf"] = new object[]
            {
                new Dictionary<string, object>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object> { ["type"] = "integer", ["minimum"] = 0, ["maximum"] = 255 },
                    ["minItems"] = 3,
                    ["maxItems"] = 3,
                },
                new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["r"] = new Dictionary<string, object> { ["type"] = "integer", ["minimum"] = 0, ["maximum"] = 255 },
                        ["g"] = new Dictionary<string, object> { ["type"] = "integer", ["minimum"] = 0, ["maximum"] = 255 },
                        ["b"] = new Dictionary<string, object> { ["type"] = "integer", ["minimum"] = 0, ["maximum"] = 255 },
                    },
                    ["required"] = new[] { "r", "g", "b" },
                    ["additionalProperties"] = false,
                },
            },
        };
}
