using System.Text.Json;
using Algomim.Aec.Mcp.Core.Operations.Create;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;
using Algomim.Revit.Mcp.Tools.Common;
using Algomim.Revit.Mcp.Tools.Composition;

namespace Algomim.Revit.Mcp.Tools.Create.TextNotesCreate;

internal sealed class TextNotesCreateTool : RevitPlannedWriteToolBase<TextNotesCreatePlan>
{
    private readonly RevitTextNotesCreateExecutor _executor;

    private TextNotesCreateTool(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore, RevitTextNotesCreateExecutor executor)
        : base(dispatcher, documentContextStore) => _executor = executor;

    public override string Name => "create_text_notes";
    public override string Description => "Create text notes in a view.";
    public override JsonElement InputSchema { get; } = Schema.From(new
    {
        type = "object",
        properties = new { viewId = new { type = "integer" }, texts = ArrayOf("string"), x = ArrayOf("number"), y = ArrayOf("number"), z = ArrayOf("number") },
        required = new[] { "viewId", "texts", "x", "y", "z" },
    });
    public override ToolMetadata Metadata { get; } = new("create_text_notes", ToolCategory.Create, ToolMode.Write, ToolRisk.High, "Create text notes in a view.");

    public static IMcpTool Create(RevitToolServices services)
        => new TextNotesCreateTool(services.Dispatcher, services.DocumentContextStore, new RevitTextNotesCreateExecutor());

    protected override TextNotesCreatePlan CreatePlan(ArgumentReader arguments)
    {
        var args = TextNotesCreateArgs.From(arguments);
        return TextNotesCreatePlanner.CreatePlan(args.ViewId, args.Texts, args.X, args.Y, args.Z);
    }

    protected override McpToolResult ExecutePlan(RevitToolContext context, TextNotesCreatePlan plan)
        => _executor.Execute(context, plan);

    private static object ArrayOf(string type)
        => new { type = "array", items = new { type } };
}
