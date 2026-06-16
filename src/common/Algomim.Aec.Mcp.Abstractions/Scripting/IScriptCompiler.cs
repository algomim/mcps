using System.Reflection;
using System.Runtime.Loader;

namespace Algomim.Aec.Mcp.Scripting;

/// <summary>Compiles a user C# script (method body) into a runnable assembly.</summary>
public interface IScriptCompiler
{
    ScriptCompilationResult Compile(string userCode);
}

/// <summary>
/// Outcome of a compilation. On success carries the loaded <see cref="Assembly"/> together with the
/// collectible <see cref="AssemblyLoadContext"/> it was loaded into, so the cache can
/// <see cref="AssemblyLoadContext.Unload"/> it on eviction.
/// </summary>
public sealed class ScriptCompilationResult
{
    public bool Success { get; }
    public Assembly? Assembly { get; }
    public AssemblyLoadContext? LoadContext { get; }
    public IReadOnlyList<string> Errors { get; }

    private ScriptCompilationResult(bool success, Assembly? assembly, AssemblyLoadContext? loadContext, IReadOnlyList<string>? errors)
    {
        Success = success;
        Assembly = assembly;
        LoadContext = loadContext;
        Errors = errors ?? Array.Empty<string>();
    }

    public static ScriptCompilationResult Succeeded(Assembly assembly, AssemblyLoadContext loadContext)
        => new(true, assembly, loadContext, null);

    public static ScriptCompilationResult Failed(IReadOnlyList<string> errors)
        => new(false, null, null, errors);
}
