using Algomim.Aec.Mcp.Hosting;

namespace Algomim.Rhino.Mcp.Hosting;

/// <summary>Rhino-specific MCP identity and client-facing instructions.</summary>
public static class RhinoMcpServerProfile
{
    public const string Owner = "rhino";
    public const string ServerName = "rhino-mcp";

    public const string ServerInstructions =
        "rhino-mcp controls McNeel Rhino in-process through a typed C#/.NET plugin.\n" +
        "Use domain_action_object lower_snake_case tools when they are available. Tools are implemented with RhinoCommon " +
        "inside the Rhino host adapter; Grasshopper and script execution are added only as explicit typed modules.\n" +
        "This initial adapter skeleton starts the MCP transport and lifecycle commands before public Rhino tools are registered.";

    public static McpHostProfile Create()
        => new(Owner, ServerName, ServerInstructions);
}
