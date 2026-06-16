using System.Text.Json;
using Algomim.Aec.Mcp.Tooling;

namespace Algomim.AutoCad.Mcp.Tools.Common;

internal static class AutoCadSchemas
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

    public static object PointArray(string description, bool withBulge = false)
    {
        var pointProps = Props(
            ("x", "number", "X coordinate"),
            ("y", "number", "Y coordinate"),
            ("z", "number", "Z coordinate"));
        if (withBulge)
            pointProps["bulge"] = new Dictionary<string, object> { ["type"] = "number", ["description"] = "Polyline bulge value" };

        return new Dictionary<string, object>
        {
            ["type"] = "array",
            ["description"] = description,
            ["items"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = pointProps,
                ["required"] = new[] { "x", "y" },
            },
        };
    }
}
