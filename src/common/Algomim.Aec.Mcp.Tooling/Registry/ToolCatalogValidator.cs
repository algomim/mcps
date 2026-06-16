using Algomim.Aec.Mcp.Core.Naming;

namespace Algomim.Aec.Mcp.Tooling.Registry;

/// <summary>Pure catalog checks shared by every host adapter.</summary>
public static class ToolCatalogValidator
{
    public static IReadOnlyList<string> ValidateNames(IEnumerable<string> names)
    {
        var errors = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var name in names)
        {
            var validation = ToolNamePolicy.Validate(name);
            if (!validation.IsValid)
                errors.Add($"{name}: {validation.Code} - {validation.Message}");

            if (!seen.Add(name))
                errors.Add($"{name}: DUPLICATE_TOOL_NAME - Tool names must be unique.");
        }

        return errors;
    }
}
