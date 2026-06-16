using Xunit;

namespace Algomim.Revit.Mcp.Tests;

public class RevitSmokeScriptTests
{
    [Fact]
    public void Smoke_script_covers_health_catalog_read_write_and_export_paths()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "revit-smoke.ps1"));

        Assert.Contains("/health", script);
        Assert.Contains("initialize", script);
        Assert.Contains("tools/list", script);
        Assert.Contains("document_get_info", script);
        Assert.Contains("view_get_active", script);
        Assert.Contains("selection_get", script);
        Assert.Contains("model_list_warnings", script);
        Assert.Contains("document_get_units", script);
        Assert.Contains("element_move", script);
        Assert.Contains("export_pdf", script);
        Assert.Contains("WritableElementId", script);
        Assert.Contains("ExportViewId", script);
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
}
