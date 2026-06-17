using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;

namespace Algomim.Rhino.Mcp.App;

/// <summary>Rhino command-line entry points for the MCP plugin.</summary>
public sealed class AlgomimCommand : Command
{
    public override string EnglishName => "Algomim";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        if (RhinoMcpPlugin.Instance is not { } app)
        {
            RhinoApp.WriteLine("rhino-mcp is not initialized.");
            return Result.Failure;
        }

        var options = new GetOption();
        options.SetCommandPrompt("Algomim MCP");
        var connect = app.IsConnected ? -1 : options.AddOption("Connect");
        var disconnect = app.IsConnected ? options.AddOption("Disconnect") : -1;
        var status = options.AddOption("Status");
        var update = options.AddOption("Update");
        options.AcceptNothing(true);

        var result = options.Get();
        if (result == GetResult.Nothing)
        {
            app.ShowStatus();
            return Result.Success;
        }

        if (result != GetResult.Option || options.Option() is not { } selected)
            return Result.Cancel;

        if (selected.Index == connect)
            app.Connect(doc);
        else if (selected.Index == disconnect)
            app.Disconnect();
        else if (selected.Index == status)
            app.ShowStatus();
        else if (selected.Index == update)
            app.CheckForUpdates();
        else
            return Result.Cancel;

        return Result.Success;
    }
}
