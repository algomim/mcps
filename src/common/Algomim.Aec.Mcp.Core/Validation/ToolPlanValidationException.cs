namespace Algomim.Aec.Mcp.Core.Validation;

/// <summary>Raised when a pure operation planner rejects invalid tool inputs.</summary>
public sealed class ToolPlanValidationException : Exception
{
    public string Field { get; }

    public ToolPlanValidationException(string field, string message)
        : base($"Invalid argument '{field}': {message}.") => Field = field;
}
