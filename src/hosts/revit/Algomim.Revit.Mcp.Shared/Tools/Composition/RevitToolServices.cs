using Algomim.Aec.Mcp.Scripting;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Composition;

/// <summary>Dependencies available to Revit tool modules at composition time.</summary>
internal sealed record RevitToolServices(
    IUiThreadDispatcher Dispatcher,
    IScriptCompiler ScriptCompiler,
    ILogger Logger,
    IRevitDocumentContextStore DocumentContextStore);
