using Algomim.Aec.Mcp.Scripting;

namespace Algomim.Revit.Mcp.Scripting;

/// <summary>
/// LRU cache over an inner <see cref="IScriptCompiler"/>, keyed by SHA-256 of the source. Caches only
/// successful compilations (errors pass through so iterative fixes recompile). On eviction the entry's
/// collectible load context is unloaded, bounding memory across long Revit sessions.
/// Touching an entry on hit moves it to most-recently-used, so the about-to-execute script is never
/// the one evicted by a concurrent compile.
/// </summary>
public sealed class CollectibleScriptCache : IScriptCompiler
{
    private const int Capacity = 100;

    private readonly IScriptCompiler _inner;
    private readonly object _gate = new();
    private readonly LinkedList<Entry> _lru = new();
    private readonly Dictionary<string, LinkedListNode<Entry>> _map = new();

    public CollectibleScriptCache(IScriptCompiler inner) => _inner = inner;

    public ScriptCompilationResult Compile(string userCode)
    {
        var key = CompileCacheKey.Sha256(userCode);

        lock (_gate)
        {
            if (_map.TryGetValue(key, out var hit))
            {
                _lru.Remove(hit);
                _lru.AddFirst(hit);
                return hit.Value.Result;
            }
        }

        var result = _inner.Compile(userCode);
        if (!result.Success) return result;

        lock (_gate)
        {
            if (_map.TryGetValue(key, out var raced))
            {
                _lru.Remove(raced);
                _lru.AddFirst(raced);
                result.LoadContext?.Unload(); // discard the duplicate we just built
                return raced.Value.Result;
            }

            var node = new LinkedListNode<Entry>(new Entry(key, result));
            _lru.AddFirst(node);
            _map[key] = node;

            if (_map.Count > Capacity)
            {
                var evicted = _lru.Last!;
                _lru.RemoveLast();
                _map.Remove(evicted.Value.Key);
                evicted.Value.Result.LoadContext?.Unload();
            }
        }

        return result;
    }

    private readonly record struct Entry(string Key, ScriptCompilationResult Result);
}
