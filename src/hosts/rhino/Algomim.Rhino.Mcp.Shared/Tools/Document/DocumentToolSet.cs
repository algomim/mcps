using Algomim.Aec.Mcp.Tooling;
using Algomim.Rhino.Mcp.Tools.Common;
using Rhino;

namespace Algomim.Rhino.Mcp.Tools.Document;

internal static class DocumentToolSet
{
    public static IEnumerable<IMcpTool> Create()
    {
        yield return GetInfo();
        yield return Save();
        yield return SaveAs();
    }

    private static IMcpTool GetInfo()
        => new DelegateRhinoTool(
            "document_get_info",
            "Gets active Rhino document context: Rhino version, document path, units, active view, object count, and selection count.",
            RhinoSchemas.Empty,
            _ => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var selection = document.Objects.GetSelectedObjects(false, false).ToArray();
                var objectCount = document.Objects
                    .GetObjectList(RhinoObjectFilters.ActiveObjects(includeHidden: true, includeLocked: true))
                    .Count();
                var activeViewport = document.Views.ActiveView?.ActiveViewport;

                var data = new
                {
                    rhino = new
                    {
                        version = RhinoApp.Version.ToString(),
                    },
                    document = new
                    {
                        name = EmptyToNull(document.Name),
                        path = EmptyToNull(document.Path),
                        runtimeSerialNumber = document.RuntimeSerialNumber,
                        isModified = document.Modified,
                        modelUnitSystem = document.ModelUnitSystem.ToString(),
                        modelAbsoluteTolerance = document.ModelAbsoluteTolerance,
                        modelAngleToleranceRadians = document.ModelAngleToleranceRadians,
                    },
                    activeView = activeViewport is null ? null : new
                    {
                        name = activeViewport.Name,
                    },
                    objects = new
                    {
                        count = objectCount,
                    },
                    selection = new
                    {
                        count = selection.Length,
                        objectIds = selection.Select(obj => obj.Id.ToString("D")).Take(100).ToArray(),
                    },
                };

                var displayName = EmptyToNull(document.Name) ?? "Untitled";
                return ToolResponse.Success(data, $"Rhino document '{displayName}' is active.");
            }));

    private static IMcpTool Save()
        => new DelegateRhinoTool(
            "document_save",
            "Saves the active Rhino document.",
            RhinoSchemas.Empty,
            _ => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                if (string.IsNullOrWhiteSpace(document.Path))
                    return ToolResponse.Failure("RHINO_DOCUMENT_PATH_REQUIRED", "Active Rhino document has no path. Use document_save_as first.");

                var ok = RhinoApp.RunScript(document.RuntimeSerialNumber, "_-Save", false);
                var data = new { path = document.Path, saved = ok };
                return ok
                    ? ToolResponse.Success(data, "Saved Rhino document.")
                    : ToolResponse.Failure("RHINO_DOCUMENT_SAVE_FAILED", "Rhino did not complete the save command.", data);
            }));

    private static IMcpTool SaveAs()
        => new DelegateRhinoTool(
            "document_save_as",
            "Saves the active Rhino document to a target .3dm path.",
            RhinoSchemas.Object(new Dictionary<string, object>(RhinoSchemas.Props(
                ("path", "string", "Target .3dm file path"))), ["path"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var path = new ArgumentReader(args).RequireString("path");
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                var commandPath = path.Replace("\"", "\\\"");
                var ok = RhinoApp.RunScript(document.RuntimeSerialNumber, $"_-SaveAs \"{commandPath}\"", false);
                var data = new { path, saved = ok };
                return ok
                    ? ToolResponse.Success(data, "Saved Rhino document as a new file.")
                    : ToolResponse.Failure("RHINO_DOCUMENT_SAVE_AS_FAILED", "Rhino did not complete the save-as command.", data);
            }));

    private static string? EmptyToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
