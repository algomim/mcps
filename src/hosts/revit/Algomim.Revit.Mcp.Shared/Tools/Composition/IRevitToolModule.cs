using Algomim.Aec.Mcp.Tooling.Composition;

namespace Algomim.Revit.Mcp.Tools.Composition;

/// <summary>Module contract for Revit-specific MCP tool groups.</summary>
internal interface IRevitToolModule : IToolModule<RevitToolServices>;
