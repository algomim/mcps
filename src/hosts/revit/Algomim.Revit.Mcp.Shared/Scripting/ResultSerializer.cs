using System.Text.Json;
using Algomim.Aec.Mcp.Tooling;

namespace Algomim.Revit.Mcp.Scripting;

/// <summary>Serializes a script's return value: primitives as-is, objects as JSON, Revit objects via ToString.</summary>
internal static class ResultSerializer
{
    public static string Serialize(object? result)
    {
        if (result is null) return "null";
        if (result is string s) return s;
        if (result is bool or int or long or double or float or decimal) return result.ToString()!;

        try
        {
            return JsonSerializer.Serialize(result, McpJson.Default);
        }
        catch
        {
            return result.ToString() ?? "null";
        }
    }
}
