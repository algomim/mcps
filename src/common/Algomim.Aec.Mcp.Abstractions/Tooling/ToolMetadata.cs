namespace Algomim.Aec.Mcp.Tooling;

/// <summary>High-level grouping used to keep a growing tool catalog navigable.</summary>
public enum ToolCategory
{
    Api,
    Scripting,
    Document,
    Category,
    Element,
    Family,
    Type,
    Parameter,
    View,
    Sheet,
    Selection,
    Graphics,
    Geometry,
    Workset,
    Analysis,
    Modify,
    Create,
    Export,
}

/// <summary>Execution mode expected by a tool.</summary>
public enum ToolMode
{
    Read,
    Write,
}

/// <summary>Risk level used for UX, validation, and future approval policies.</summary>
public enum ToolRisk
{
    Low,
    Medium,
    High,
    Destructive,
}

/// <summary>Optional metadata exposed by typed tools beyond the MCP wire definition.</summary>
public sealed record ToolMetadata(
    string Name,
    ToolCategory Category,
    ToolMode Mode,
    ToolRisk Risk,
    string Description);

/// <summary>Implemented by tools that carry catalog metadata.</summary>
public interface IToolMetadataProvider
{
    ToolMetadata Metadata { get; }
}
