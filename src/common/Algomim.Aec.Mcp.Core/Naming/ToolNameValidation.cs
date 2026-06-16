namespace Algomim.Aec.Mcp.Core.Naming;

/// <summary>Pure result object returned by tool naming validation.</summary>
public sealed record ToolNameValidation(bool IsValid, string? Code = null, string? Message = null)
{
    public static ToolNameValidation Valid() => new(true);

    public static ToolNameValidation Invalid(string code, string message) => new(false, code, message);
}
