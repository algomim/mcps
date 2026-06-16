using System.Linq;

namespace Algomim.Revit.Mcp.Discovery;

/// <summary>
/// Answers api_discover queries against the live Revit API via reflection — version-correct by
/// construction. Accepts a single <c>query</c>: an exact type name, a <c>Type.Member</c> form, or
/// free text. Reflection-only, so it does not need the UI thread.
/// </summary>
public static class ReflectionApiDiscovery
{
    public static void WarmUp() => RevitTypeCache.WarmUp();

    public static object Discover(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new { error = "Provide a query: a type name (e.g. 'Wall'), a 'Type.Member' (e.g. 'View.CropBox'), or free text." };

        var types = RevitTypeCache.GetRevitTypes();
        query = query.Trim();

        // 'Type.Member' form — only if the part before the last dot resolves to a known type.
        string typeName = query;
        string? member = null;
        var dot = query.LastIndexOf('.');
        if (dot > 0)
        {
            var candidateType = query[..dot];
            if (types.Any(t => NameEquals(t, candidateType)))
            {
                typeName = candidateType;
                member = query[(dot + 1)..];
            }
        }

        // Mode 1: exact type inspection.
        var type = types.FirstOrDefault(t => NameEquals(t, typeName));
        if (type != null)
            return RevitTypeInspector.InspectType(type, member);

        // Mode 2: free-text search across type names, with fuzzy suggestions.
        var matches = types
            .Where(t => t.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderBy(t => t.Name.Length)
            .Take(20)
            .Select(t => new
            {
                name = t.Name,
                fullName = t.FullName,
                kind = t.IsEnum ? "enum" : t.IsInterface ? "interface" : t.IsAbstract ? "abstract class" : "class",
            })
            .ToList();

        if (matches.Count == 0)
            return new { query, matches = Array.Empty<object>(), hint = "No matching Revit types. Try a shorter or different term." };

        return new { query, matches };
    }

    private static bool NameEquals(Type t, string name) =>
        string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(t.FullName, name, StringComparison.OrdinalIgnoreCase);
}
