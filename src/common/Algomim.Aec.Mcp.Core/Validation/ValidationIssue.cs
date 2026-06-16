namespace Algomim.Aec.Mcp.Core.Validation;

/// <summary>Pure validation issue used before host API side effects begin.</summary>
public sealed record ValidationIssue(string Code, string Message, string? Field = null);
