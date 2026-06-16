using System.Text.Json;
using Autodesk.Revit.UI;
using Algomim.Revit.Mcp.Harness;
using Algomim.Aec.Mcp.Tooling;

namespace Algomim.Revit.Mcp.Tools.Common;

/// <summary>Small descriptor-backed typed tool for catalog entries that do not need a dedicated class.</summary>
public sealed class DelegateRevitTool : RevitToolBase
{
    private readonly Func<UIApplication, ArgumentReader, McpToolResult> _execute;

    public DelegateRevitTool(
        IUiThreadDispatcher dispatcher,
        IRevitDocumentContextStore documentContextStore,
        string name,
        string description,
        JsonElement inputSchema,
        ToolMetadata metadata,
        Func<UIApplication, ArgumentReader, McpToolResult> execute)
        : base(dispatcher, documentContextStore)
    {
        Name = name;
        Description = description;
        InputSchema = inputSchema;
        Metadata = metadata;
        _execute = execute;
    }

    public override string Name { get; }
    public override string Description { get; }
    public override JsonElement InputSchema { get; }
    public override ToolMetadata Metadata { get; }

    protected override McpToolResult ExecuteOnUiThread(UIApplication uiApp, ArgumentReader arguments)
        => _execute(uiApp, arguments);
}
