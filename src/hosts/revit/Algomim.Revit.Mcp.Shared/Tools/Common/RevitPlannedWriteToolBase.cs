using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Harness;

namespace Algomim.Revit.Mcp.Tools.Common;

/// <summary>Template for write tools that produce a pure plan before touching the Revit API.</summary>
public abstract class RevitPlannedWriteToolBase<TPlan> : RevitWriteToolBase
{
    protected RevitPlannedWriteToolBase(IUiThreadDispatcher dispatcher, IRevitDocumentContextStore documentContextStore)
        : base(dispatcher, documentContextStore)
    {
    }

    protected sealed override McpToolResult ExecuteWrite(RevitToolContext context, ArgumentReader arguments)
        => ExecutePlan(context, CreatePlan(arguments));

    protected abstract TPlan CreatePlan(ArgumentReader arguments);

    protected abstract McpToolResult ExecutePlan(RevitToolContext context, TPlan plan);
}
