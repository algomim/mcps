using System.Reflection;
using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Algomim.Revit.Mcp.Harness;
using Algomim.Aec.Mcp.Scripting;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Scripting;
using RevitDocument = Autodesk.Revit.DB.Document;
using RevitView = Autodesk.Revit.DB.View;

namespace Algomim.Revit.Mcp.Tools;

/// <summary>
/// Compiles and runs a user C# method body against the active Revit document. Compilation happens on
/// the calling (HTTP) thread; execution is marshalled to the Revit UI thread. <c>write</c> mode wraps
/// the run in an undoable transaction with auto-rollback; nothing escapes into Revit.
/// </summary>
public sealed class ExecuteScriptTool : IMcpTool
{
    private const int TimeoutMs = 120_000;

    private readonly IScriptCompiler _compiler;
    private readonly IUiThreadDispatcher _dispatcher;

    public ExecuteScriptTool(IScriptCompiler compiler, IUiThreadDispatcher dispatcher)
    {
        _compiler = compiler;
        _dispatcher = dispatcher;
    }

    public string Name => "execute-script";

    public string Description =>
        "Run C# against the active Revit document. 'code' is a method body (not a full class); return a " +
        "value to report it. In scope: doc (Document), uidoc (UIDocument), activeView (View), uiApp " +
        "(UIApplication), p (RevitParams — read 'params' via p.GetString/GetInt/GetDouble/...). Do NOT write " +
        "using-directives; common namespaces are pre-imported. mode='write' (default 'read') wraps the run in " +
        "an undoable transaction — required for any change. Call api_discover first if unsure about an API.";

    public JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new
        {
            code = new { type = "string", description = "C# method body. Return a value to report it." },
            mode = new { type = "string", @enum = new[] { "read", "write" }, description = "'write' for changes (transaction), else 'read'. Default 'read'." },
            @params = new { type = "object", description = "Optional values readable in the script via p (e.g. p.GetDouble(\"height\"))." },
        },
        required = new[] { "code" },
    });

    public async Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        if (arguments is not { } args || !args.TryGetProperty("code", out var codeEl) || codeEl.GetString() is not { Length: > 0 } code)
            return McpToolResult.Error("Missing required parameter 'code'.");

        var write = args.TryGetProperty("mode", out var modeEl) &&
                    string.Equals(modeEl.GetString(), "write", StringComparison.OrdinalIgnoreCase);

        var paramsData = args.TryGetProperty("params", out var p) && p.ValueKind == JsonValueKind.Object
            ? p.Clone()
            : EmptyObject();

        var compilation = _compiler.Compile(code);
        if (!compilation.Success)
            return McpToolResult.Error("[COMPILE_ERROR]\n" + string.Join("\n", compilation.Errors));

        var assembly = compilation.Assembly!;

        try
        {
            var run = _dispatcher.InvokeOnUiThreadAsync(uiApp => RunOnUiThread(uiApp, assembly, write, paramsData));
            if (await Task.WhenAny(run, Task.Delay(TimeoutMs)) != run)
                return McpToolResult.Error("[RUNTIME_ERROR] Script timed out — it may be blocked on a dialog or a long operation.");

            return await run;
        }
        catch (Exception ex)
        {
            return McpToolResult.Error($"[RUNTIME_ERROR] {ex.GetType().Name}: {ex.Message}");
        }
    }

    // Runs on the Revit UI thread. Pure-synchronous — no awaits.
    private static McpToolResult RunOnUiThread(UIApplication uiApp, Assembly assembly, bool write, JsonElement paramsData)
    {
        var uidoc = uiApp.ActiveUIDocument;
        if (uidoc?.Document is not { } doc)
            return McpToolResult.Error("[RUNTIME_ERROR] No active Revit document.");

        var view = uidoc.ActiveView;
        if (view != null && ViewValidator.ValidateForScriptExecution(view) is { } viewError)
        {
            view = ViewValidator.TryAutoRecover(doc, uidoc);
            if (view is null)
                return McpToolResult.Error("[RUNTIME_ERROR] " + viewError);
        }

        var p = new RevitParams(paramsData);

        return write
            ? TransactionRunner.Run(doc, "revit-mcp script_execute", () => Invoke(assembly, doc, uidoc, view!, uiApp, p))
            : Invoke(assembly, doc, uidoc, view!, uiApp, p);
    }

    private static McpToolResult Invoke(Assembly assembly, RevitDocument doc, UIDocument uidoc, RevitView view, UIApplication uiApp, RevitParams p)
    {
        try
        {
            var result = ScriptExecutor.Invoke(assembly, doc, uidoc, view, uiApp, p);
            return McpToolResult.Success(ResultSerializer.Serialize(result));
        }
        catch (TargetInvocationException tie)
        {
            var inner = tie.InnerException ?? tie;
            return McpToolResult.Error($"[RUNTIME_ERROR] {inner.GetType().Name}: {inner.Message}");
        }
        catch (Exception ex)
        {
            return McpToolResult.Error($"[RUNTIME_ERROR] {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static JsonElement EmptyObject()
    {
        using var doc = JsonDocument.Parse("{}");
        return doc.RootElement.Clone();
    }
}
