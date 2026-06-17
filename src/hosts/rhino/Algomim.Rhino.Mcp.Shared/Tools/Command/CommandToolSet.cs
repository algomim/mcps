using Algomim.Aec.Mcp.Tooling;
using Algomim.Rhino.Mcp.Tools.Common;
using Rhino;
using Rhino.Commands;

namespace Algomim.Rhino.Mcp.Tools.Command;

internal static class CommandToolSet
{
    public static IEnumerable<IMcpTool> Create()
    {
        yield return List();
        yield return Run();
    }

    private static IMcpTool List()
        => new DelegateRhinoTool(
            "command_list",
            "Lists Rhino commands by optional name filter.",
            RhinoSchemas.Object(new Dictionary<string, object>(RhinoSchemas.Props(
                ("filter", "string", "Optional case-insensitive command name filter")))
            {
                ["loadedOnly"] = RhinoSchemas.Boolean("Only include commands from loaded plug-ins", defaultValue: true),
                ["limit"] = RhinoSchemas.Integer("Maximum commands to return", 1, 5000, 500),
            }),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(_ =>
            {
                var reader = new ArgumentReader(args);
                var filter = reader.GetString("filter");
                var loadedOnly = reader.GetBool("loadedOnly", fallback: true);
                var limit = Math.Clamp(reader.GetInt("limit", 500), 1, 5000);

                var names = global::Rhino.Commands.Command.GetCommandNames(english: true, loaded: loadedOnly) ?? Array.Empty<string>();
                var commands = names
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Where(name => string.IsNullOrWhiteSpace(filter) || name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .Take(limit + 1)
                    .ToArray();

                var truncated = commands.Length > limit;
                var data = new
                {
                    count = truncated ? limit : commands.Length,
                    truncated,
                    loadedOnly,
                    commands = truncated ? commands.Take(limit).ToArray() : commands,
                };
                return ToolResponse.Success(data, $"Listed {data.count} Rhino command(s).");
            }));

    private static IMcpTool Run()
        => new DelegateRhinoTool(
            "command_run",
            "Runs a Rhino command string and returns captured command-window output.",
            RhinoSchemas.Object(new Dictionary<string, object>(RhinoSchemas.Props(
                ("command", "string", "Rhino command script, for example: _Box 0,0,0 5,5,5"),
                ("echo", "boolean", "Echo the command in Rhino"))), ["command"]),
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document =>
            {
                var reader = new ArgumentReader(args);
                var command = reader.RequireString("command");
                if (global::Rhino.Commands.Command.InCommand())
                    return ToolResponse.Failure(
                        "RHINO_COMMAND_BUSY",
                        "Rhino is already running a command. Finish or cancel the active command before running another command.");

                var previousCapture = RhinoApp.CommandWindowCaptureEnabled;
                RhinoApp.CommandWindowCaptureEnabled = true;
                var ran = false;
                string[] output = [];
                Exception? runError = null;

                try
                {
                    try
                    {
                        ran = RhinoApp.RunScript(document.RuntimeSerialNumber, command, reader.GetBool("echo"));
                    }
                    catch (Exception ex)
                    {
                        runError = ex;
                    }

                    output = RhinoApp.CapturedCommandWindowStrings(true) ?? Array.Empty<string>();
                }
                finally
                {
                    RhinoApp.CommandWindowCaptureEnabled = previousCapture;
                }

                var data = new
                {
                    command,
                    success = ran && runError is null,
                    output = output.Length == 0 ? string.Empty : string.Join(Environment.NewLine, output),
                    error = runError?.Message,
                };

                document.Views.Redraw();
                return data.success
                    ? ToolResponse.Success(data, "Rhino command completed.")
                    : ToolResponse.Failure("RHINO_COMMAND_FAILED", "Rhino command did not complete successfully.", data);
            }));
}
