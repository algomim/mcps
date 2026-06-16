using System.Reflection;
using System.Runtime.Loader;

namespace Algomim.Revit.Mcp.Scripting;

/// <summary>
/// Collectible load context for a single compiled script, so its assembly can be unloaded when the
/// compile cache evicts it. Shared dependencies (Revit API, framework, plugin assemblies) resolve
/// from the default context — only the script's own emitted bytes live here.
/// </summary>
internal sealed class ScriptAssemblyLoadContext : AssemblyLoadContext
{
    public ScriptAssemblyLoadContext() : base(isCollectible: true) { }

    protected override Assembly? Load(AssemblyName assemblyName) => null;
}
