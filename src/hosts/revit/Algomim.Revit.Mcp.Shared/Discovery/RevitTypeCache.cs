using System.Linq;

namespace Algomim.Revit.Mcp.Discovery;

/// <summary>Thread-safe, lazily-built cache of public types from the loaded Revit API assemblies.</summary>
internal static class RevitTypeCache
{
    private static Type[]? _types;
    private static readonly object _gate = new();

    /// <summary>Builds the cache off the UI thread so the first api_discover call is fast.</summary>
    public static void WarmUp() => Task.Run(() => GetRevitTypes());

    public static Type[] GetRevitTypes()
    {
        if (_types != null) return _types;

        lock (_gate)
        {
            if (_types != null) return _types;

            var revitAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a =>
                {
                    var name = a.GetName().Name;
                    return name != null &&
                        (name.StartsWith("RevitAPI", StringComparison.OrdinalIgnoreCase) ||
                         name.StartsWith("Autodesk.Revit", StringComparison.OrdinalIgnoreCase));
                });

            _types = revitAssemblies
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                .Where(t => t.IsPublic)
                .ToArray();

            return _types;
        }
    }
}
