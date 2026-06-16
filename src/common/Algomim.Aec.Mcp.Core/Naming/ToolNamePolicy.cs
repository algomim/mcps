using System.Text.RegularExpressions;

namespace Algomim.Aec.Mcp.Core.Naming;

/// <summary>Pure naming rules for public MCP tools across all AEC host adapters.</summary>
public static partial class ToolNamePolicy
{
    public const string Separator = "_";

    private static readonly HashSet<string> AllowedDomains = new(StringComparer.Ordinal)
    {
        "api",
        "analysis",
        "annotation",
        "block",
        "category",
        "dimension",
        "document",
        "drawing",
        "element",
        "entity",
        "export",
        "family",
        "geometry",
        "graphics",
        "grid",
        "layer",
        "level",
        "material",
        "measurement",
        "model",
        "parameter",
        "property",
        "schedule",
        "script",
        "selection",
        "sheet",
        "tag",
        "type",
        "view",
        "workset",
        "worksharing",
    };

    public static bool IsAllowedDomain(string domain) => AllowedDomains.Contains(domain);

    public static IReadOnlyCollection<string> Domains => AllowedDomains;

    public static ToolNameValidation Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ToolNameValidation.Invalid("TOOL_NAME_EMPTY", "Tool name cannot be empty.");

        if (!SnakeCaseRegex().IsMatch(name))
            return ToolNameValidation.Invalid("TOOL_NAME_FORMAT", "Tool name must be lower_snake_case.");

        var domain = GetDomain(name);
        if (!IsAllowedDomain(domain))
            return ToolNameValidation.Invalid("TOOL_NAME_DOMAIN", $"Unsupported tool domain '{domain}'.");

        if (name.Split(Separator, StringSplitOptions.RemoveEmptyEntries).Length < 2)
            return ToolNameValidation.Invalid("TOOL_NAME_SHAPE", "Tool name must include at least domain and action.");

        return ToolNameValidation.Valid();
    }

    public static string GetDomain(string name)
    {
        var index = name.IndexOf(Separator, StringComparison.Ordinal);
        return index < 0 ? name : name[..index];
    }

    [GeneratedRegex("^[a-z][a-z0-9]*(?:_[a-z0-9]+)*$")]
    private static partial Regex SnakeCaseRegex();
}
