namespace Algomim.Aec.Mcp.Core.Validation;

/// <summary>Pure validation result for args, plans, and naming policies.</summary>
public sealed record ValidationResult(IReadOnlyList<ValidationIssue> Issues)
{
    public bool IsValid => Issues.Count == 0;

    public static ValidationResult Valid() => new(Array.Empty<ValidationIssue>());

    public static ValidationResult Invalid(params ValidationIssue[] issues) => new(issues);
}
