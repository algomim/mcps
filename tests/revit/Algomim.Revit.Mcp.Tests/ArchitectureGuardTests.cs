using System.Text.RegularExpressions;
using System.Text.Json;
using Algomim.Aec.Mcp.Core.Naming;
using Xunit;

namespace Algomim.Revit.Mcp.Tests;

public class ArchitectureGuardTests
{
    private static readonly string[] HostSpecificTerms =
    [
        "Autodesk",
        "Revit",
        "AutoCAD",
        "AutoCad",
        "Rhino",
        "Cart" + "Cut",
        "cart" + "cut"
    ];

    private static readonly Regex[] ToolNamePatterns =
    [
        new(@"yield\s+return\s+(?:Tool|Read|Write)\s*\([^,]+,\s*""(?<name>[^""]+)""", RegexOptions.Singleline),
        new(@"new\s+DelegateRevitTool\s*\(\s*[^,]+,\s*[^,]+,\s*""(?<name>[^""]+)""", RegexOptions.Singleline),
        new(@"Name\s*=>\s*""(?<name>[^""]+)"""),
        new(@"new\s+McpToolAlias\s*\(\s*""(?<name>[^""]+)""", RegexOptions.Singleline),
        new(@"AddAlias\s*\(\s*tools,\s*""(?<name>[^""]+)""")
    ];

    [Fact]
    public void Common_aec_packages_stay_host_neutral()
    {
        var root = FindRepositoryRoot();
        var commonDirs = Directory.GetDirectories(Path.Combine(root, "src", "common"), "Algomim.Aec.Mcp.*");

        var offenders = commonDirs
            .SelectMany(dir => Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
            .Where(IsSourceFile)
            .SelectMany(file => HostSpecificTerms
                .Where(term => File.ReadAllText(file).Contains(term, StringComparison.Ordinal))
                .Select(term => $"{Relative(root, file)} contains '{term}'"))
            .ToList();

        Assert.True(offenders.Count == 0, string.Join(Environment.NewLine, offenders));
    }

    [Fact]
    public void Removed_host_placeholder_does_not_return()
    {
        var root = FindRepositoryRoot();
        var removedHostDir = Path.Combine(root, "installer", "hosts", "cart" + "cut");

        Assert.False(Directory.Exists(removedHostDir), "Removed host installer placeholder should not exist.");
    }

    [Fact]
    public void Source_projects_stay_inside_monorepo_buckets()
    {
        var root = FindRepositoryRoot();
        var sourceRoot = Path.Combine(root, "src");
        var misplacedProjects = Directory.EnumerateDirectories(sourceRoot, "Algomim.*", SearchOption.TopDirectoryOnly)
            .Select(path => Relative(root, path))
            .ToList();

        Assert.True(misplacedProjects.Count == 0, "Projects must live under src/common or src/hosts/<host>: " + string.Join(", ", misplacedProjects));
    }

    [Fact]
    public void Revit_tool_names_follow_policy_or_explicit_legacy_aliases()
    {
        var root = FindRepositoryRoot();
        var toolsDir = Path.Combine(root, "src", "hosts", "revit", "Algomim.Revit.Mcp.Shared", "Tools");
        var names = Directory.EnumerateFiles(toolsDir, "*.cs", SearchOption.AllDirectories)
            .SelectMany(ReadToolNames)
            .ToList();

        var invalid = names
            .Where(name => !ToolNamePolicy.Validate(name).IsValid && !IsAllowedLegacyToolName(name))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
        var duplicates = names
            .GroupBy(name => name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(invalid.Count == 0, "Invalid tool names: " + string.Join(", ", invalid));
        Assert.True(duplicates.Count == 0, "Duplicate tool names in source declarations: " + string.Join(", ", duplicates));
    }

    [Fact]
    public void Revit_public_tool_catalog_stays_regression_locked()
    {
        var root = FindRepositoryRoot();
        var toolsDir = Path.Combine(root, "src", "hosts", "revit", "Algomim.Revit.Mcp.Shared", "Tools");
        var actual = Directory.EnumerateFiles(toolsDir, "*.cs", SearchOption.AllDirectories)
            .SelectMany(ReadToolNames)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        var expected = ExpectedRevitToolNames.OrderBy(name => name, StringComparer.Ordinal).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Revit_runtime_cleanup_flow_removes_announcements_on_disconnect_and_shutdown()
    {
        var root = FindRepositoryRoot();
        var app = File.ReadAllText(Path.Combine(root, "src", "hosts", "revit", "Algomim.Revit.Mcp.Shared", "App", "RevitMcpApp.cs"));
        var writer = File.ReadAllText(Path.Combine(root, "src", "hosts", "revit", "Algomim.Revit.Mcp.Shared", "Hosting", "AnnouncementWriter.cs"));

        Assert.Contains("Disconnect();", app);
        Assert.Contains("_announcements?.Remove(Environment.ProcessId);", app);
        Assert.Contains("DeleteIfExists(_filePath);", writer);
        Assert.Contains("revit-{pid}-*.json", writer);
    }

    [Fact]
    public void Host_ribbons_expose_standard_connection_status_and_update_actions()
    {
        var root = FindRepositoryRoot();
        var contract = File.ReadAllText(Path.Combine(root, "docs", "HOST_CONTRACT.md"));
        var revitRibbon = File.ReadAllText(Path.Combine(root, "src", "hosts", "revit", "Algomim.Revit.Mcp.Shared", "UI", "RibbonController.cs"));
        var revitStatus = File.ReadAllText(Path.Combine(root, "src", "hosts", "revit", "Algomim.Revit.Mcp.Shared", "UI", "ShowStatusCommand.cs"));
        var revitApp = File.ReadAllText(Path.Combine(root, "src", "hosts", "revit", "Algomim.Revit.Mcp.Shared", "App", "RevitMcpApp.cs"));
        var autoCadRibbon = File.ReadAllText(Path.Combine(root, "src", "hosts", "autocad", "Algomim.AutoCad.Mcp.Shared", "UI", "RibbonController.cs"));
        var autoCadApp = File.ReadAllText(Path.Combine(root, "src", "hosts", "autocad", "Algomim.AutoCad.Mcp.Shared", "App", "AutoCadMcpApp.cs"));
        var autoCadCommands = File.ReadAllText(Path.Combine(root, "src", "hosts", "autocad", "Algomim.AutoCad.Mcp.Shared", "App", "AutoCadMcpCommands.cs"));

        Assert.Contains("Connect/Disconnect", contract);
        Assert.Contains("MCP URL: http://127.0.0.1:<port>/mcp", contract);
        Assert.Contains("Health: http://127.0.0.1:<port>/health", contract);
        Assert.Contains("<host>-mcp-X.Y.Z.msi", contract);
        Assert.Contains("tools/list", contract);
        Assert.Contains("tools/call", contract);

        Assert.Contains("revitMcpToggle", revitRibbon);
        Assert.Contains("revitMcpStatus", revitRibbon);
        Assert.Contains("revitMcpUpdate", revitRibbon);
        Assert.Contains("ShowStatusCommand", revitStatus);
        Assert.Contains("MCP URL: http://127.0.0.1:{port}/mcp", revitApp);
        Assert.Contains("Health: http://127.0.0.1:{port}/health", revitApp);
        Assert.Contains("_announcements?.Write", revitApp);
        Assert.Contains("_announcements?.Remove(Environment.ProcessId)", revitApp);
        Assert.Contains(".CheckAsync(currentVersion, \"revit-mcp-\")", revitApp);

        Assert.Contains("ButtonId", autoCadRibbon);
        Assert.Contains("StatusButtonId", autoCadRibbon);
        Assert.Contains("UpdateButtonId", autoCadRibbon);
        Assert.Contains("ShowStatus()", autoCadApp);
        Assert.Contains("MCP URL: http://127.0.0.1:{port}/mcp", autoCadApp);
        Assert.Contains("Health: http://127.0.0.1:{port}/health", autoCadApp);
        Assert.Contains("_announcements?.Write", autoCadApp);
        Assert.Contains("_announcements?.Remove(AutoCadMcpServerProfile.Owner, Environment.ProcessId)", autoCadApp);
        Assert.Contains(".CheckAsync(currentVersion, \"autocad-mcp-\")", autoCadApp);
        Assert.Contains("ALGOMIM_MCP_CONNECT", autoCadCommands);
        Assert.Contains("ALGOMIM_MCP_DISCONNECT", autoCadCommands);
        Assert.Contains("ALGOMIM_MCP_STATUS", autoCadCommands);
        Assert.Contains("ALGOMIM_MCP_CHECK_UPDATE", autoCadCommands);
    }

    [Fact]
    public void Release_host_config_requires_revit_autocad_and_rhino_artifacts()
    {
        var root = FindRepositoryRoot();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "release", "hosts.json")));
        var hosts = document.RootElement.GetProperty("releaseSupportedHosts").EnumerateArray().ToList();
        var hostIds = hosts.Select(host => host.GetProperty("id").GetString()).ToList();
        var assetPatterns = hosts
            .SelectMany(host => host.GetProperty("artifacts").EnumerateArray())
            .SelectMany(artifact => new[]
            {
                artifact.GetProperty("assetPattern").GetString(),
                artifact.GetProperty("checksumPattern").GetString()
            })
            .ToList();

        Assert.Contains("revit", hostIds);
        Assert.Contains("autocad", hostIds);
        Assert.Contains("rhino", hostIds);
        Assert.Contains("revit-mcp-{version}.msi", assetPatterns);
        Assert.Contains("autocad-mcp-{version}.msi", assetPatterns);
        Assert.Contains("rhino-mcp-{version}.msi", assetPatterns);
        Assert.Contains("rhino-mcp-{version}.msi.sha256", assetPatterns);
        Assert.Contains("algomim-rhino-mcp-{version}-rh8_*-win.yak", assetPatterns);
        Assert.Contains("algomim-rhino-mcp-{version}-rh8_*-win.yak.sha256", assetPatterns);
    }

    [Fact]
    public void Local_install_script_validates_monorepo_paths_and_cleans_stale_plugin_files()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "install-local.ps1"));

        Assert.Contains(@"src\hosts\revit\Algomim.Revit.Mcp.$Version", script);
        Assert.Contains("Test-Path $project -PathType Leaf", script);
        Assert.Contains("Algomim.Revit.Mcp.$Version.dll", script);
        Assert.Contains("Algomim.Revit.Mcp.$Version.addin", script);
        Assert.Contains("Get-ChildItem -LiteralPath $pluginDir -Force | Remove-Item -Recurse -Force", script);
    }

    private static IEnumerable<string> ReadToolNames(string file)
    {
        var text = File.ReadAllText(file);
        return ToolNamePatterns.SelectMany(pattern => pattern.Matches(text).Select(match => match.Groups["name"].Value));
    }

    private static bool IsAllowedLegacyToolName(string name)
        => name is "execute-script" or "discover-api";

    private static bool IsSourceFile(string file)
    {
        var normalized = file.Replace('\\', '/');
        return (normalized.EndsWith(".cs", StringComparison.Ordinal) || normalized.EndsWith(".csproj", StringComparison.Ordinal)) &&
               !normalized.Contains("/bin/", StringComparison.Ordinal) &&
               !normalized.Contains("/obj/", StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Algomim.Aec.Mcp.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be found.");
    }

    private static string Relative(string root, string path)
        => Path.GetRelativePath(root, path).Replace('\\', '/');

    private static readonly string[] ExpectedRevitToolNames =
    [
        "api_discover",
        "category_list",
        "category_search",
        "discover-api",
        "document_get_info",
        "document_get_units",
        "document_switch_context",
        "element_copy",
        "element_delete",
        "element_get_info",
        "element_list_by_category",
        "element_move",
        "element_rotate",
        "execute-script",
        "export_cad",
        "export_pdf",
        "family_get_file_sizes",
        "family_list",
        "family_list_by_category",
        "family_list_elements",
        "family_list_types",
        "geometry_get_boundary_lines",
        "geometry_get_bounding_boxes",
        "geometry_get_host_ids",
        "geometry_get_locations",
        "graphics_filter_test_elements",
        "graphics_get_element_overrides",
        "graphics_get_filter_overrides",
        "graphics_get_view_filters",
        "graphics_set_element_overrides",
        "grid_create",
        "level_create",
        "material_get_layers",
        "model_list_warnings",
        "parameter_get_values",
        "parameter_list",
        "parameter_set_values",
        "property_get_values",
        "property_list",
        "property_set_values",
        "schedule_create",
        "schedule_get_info",
        "script_execute",
        "selection_get",
        "selection_set",
        "sheet_create",
        "sheet_get_contents",
        "sheet_place_views",
        "sheet_set_revisions",
        "tag_create",
        "type_list_elements",
        "type_list_for_elements",
        "view_copy_filters",
        "view_create_3ds",
        "view_create_drafting_or_legend",
        "view_create_plans",
        "view_create_room_elevations",
        "view_create_sections",
        "view_create_text_notes",
        "view_get_active",
        "view_isolate_elements",
        "view_list_elements",
        "workset_get_for_elements",
        "workset_list",
        "worksharing_get_info",
    ];
}
