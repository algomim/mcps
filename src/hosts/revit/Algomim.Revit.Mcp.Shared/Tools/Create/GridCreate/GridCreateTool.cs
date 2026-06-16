using System.Text.Json;
using Algomim.Aec.Mcp.Core.Operations.Create;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Composition;

namespace Algomim.Revit.Mcp.Tools.Create.GridCreate;

internal sealed class GridCreateTool : RevitWriteToolBase
{
    private readonly RevitGridCreateExecutor _executor;

    private GridCreateTool(
        IUiThreadDispatcher dispatcher,
        IRevitDocumentContextStore documentContextStore,
        RevitGridCreateExecutor executor)
        : base(dispatcher, documentContextStore) => _executor = executor;

    public override string Name => "grid_create";
    public override string Description => "Create grid lines from names and start/end XYZ arrays.";
    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new
        {
            names = ArrayOf("string"),
            startX = ArrayOf("number"),
            startY = ArrayOf("number"),
            startZ = ArrayOf("number"),
            endX = ArrayOf("number"),
            endY = ArrayOf("number"),
            endZ = ArrayOf("number"),
        },
        required = new[] { "names", "startX", "startY", "startZ", "endX", "endY", "endZ" },
    });
    public override ToolMetadata Metadata { get; } = new("grid_create", ToolCategory.Create, ToolMode.Write, ToolRisk.High, "Create grid lines from names and start/end XYZ arrays.");

    public static IMcpTool Create(RevitToolServices services)
        => new GridCreateTool(services.Dispatcher, services.DocumentContextStore, new RevitGridCreateExecutor());

    protected override McpToolResult ExecuteWrite(RevitToolContext context, ArgumentReader arguments)
    {
        var args = GridCreateArgs.From(arguments);
        var plan = GridCreatePlanner.CreatePlan(args.Names, args.StartX, args.StartY, args.StartZ, args.EndX, args.EndY, args.EndZ);
        return _executor.Execute(context, plan);
    }

    private static object ArrayOf(string type)
        => new { type = "array", items = new { type } };
}
