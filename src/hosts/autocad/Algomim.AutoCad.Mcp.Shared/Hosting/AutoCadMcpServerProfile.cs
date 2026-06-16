using Algomim.Aec.Mcp.Hosting;

namespace Algomim.AutoCad.Mcp.Hosting;

/// <summary>AutoCAD-specific MCP identity and client-facing instructions.</summary>
public static class AutoCadMcpServerProfile
{
    public const string Owner = "autocad";
    public const string ServerName = "autocad-mcp";

    public const string ServerInstructions =
        "autocad-mcp controls Autodesk AutoCAD in-process through a typed C#/.NET plugin.\n" +
        "Use domain_action lower_snake_case tools when they are available. Tools are implemented with the AutoCAD .NET API, " +
        "not Python, LISP, SCR files, or a Node bridge.\n" +
        "The host is intentionally modular: geometry, layer, entity, block, dimension, annotation, document, export, " +
        "and analysis tool groups are added as typed C# modules.";

    public static McpHostProfile Create()
        => new(Owner, ServerName, ServerInstructions);
}
