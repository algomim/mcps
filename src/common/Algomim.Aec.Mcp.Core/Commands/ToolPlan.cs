namespace Algomim.Aec.Mcp.Core.Commands;

/// <summary>Pure operation plan produced before an adapter performs host-specific side effects.</summary>
public abstract record ToolPlan(string ToolName, ToolExecutionMode Mode);
