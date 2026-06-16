using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Algomim.Revit.Mcp.Scripting;

/// <summary>
/// Collects Roslyn <see cref="MetadataReference"/>s: framework, Revit API, and all loaded assemblies.
/// Deduplicates by simple assembly name to avoid Roslyn "same simple name already imported" errors
/// caused by third-party add-ins loading duplicate DLLs.
/// </summary>
internal static class AssemblyReferenceCollector
{
    public static List<MetadataReference> Collect()
    {
        var references = new List<MetadataReference>();
        var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var addedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void TryAddReference(string? path)
        {
            if (path != null && File.Exists(path) && addedPaths.Add(path))
                references.Add(MetadataReference.CreateFromFile(path));
        }

        void TryAddAssembly(Assembly? assembly)
        {
            if (assembly is null || assembly.IsDynamic) return;
            try
            {
                var location = assembly.Location;
                if (string.IsNullOrEmpty(location)) return;
                var simpleName = assembly.GetName().Name;
                if (simpleName != null && !addedNames.Add(simpleName)) return;
                TryAddReference(location);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[revit-mcp] reference collect failed: {ex.Message}");
            }
        }

        TryAddAssembly(typeof(object).Assembly);        // System.Runtime
        TryAddAssembly(typeof(Enumerable).Assembly);    // System.Linq
        TryAddAssembly(typeof(List<>).Assembly);        // System.Collections
        TryAddAssembly(typeof(StringBuilder).Assembly); // System.Text
        TryAddAssembly(typeof(Console).Assembly);       // System.Console

        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        if (runtimeDir != null)
        {
            foreach (var dll in new[]
            {
                "System.dll", "System.Core.dll", "System.Runtime.dll", "System.Collections.dll",
                "System.Linq.dll", "System.Linq.Expressions.dll", "netstandard.dll",
            })
            {
                TryAddReference(Path.Combine(runtimeDir, dll));
            }
        }

        TryAddAssembly(typeof(Autodesk.Revit.DB.Document).Assembly);      // RevitAPI.dll
        TryAddAssembly(typeof(Autodesk.Revit.UI.UIApplication).Assembly); // RevitAPIUI.dll

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            TryAddAssembly(assembly);

        return references;
    }
}
