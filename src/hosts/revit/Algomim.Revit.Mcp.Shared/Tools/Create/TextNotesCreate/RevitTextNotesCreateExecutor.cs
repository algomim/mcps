using Autodesk.Revit.DB;
using Algomim.Aec.Mcp.Core.Operations.Create;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Create.TextNotesCreate;

internal sealed class RevitTextNotesCreateExecutor
{
    public McpToolResult Execute(RevitToolContext context, TextNotesCreatePlan plan)
    {
        var viewId = RevitIds.Id(plan.ViewId);
        var typeId = new FilteredElementCollector(context.Document).OfClass(typeof(TextNoteType)).FirstElementId();
        var notes = new List<object>();
        foreach (var item in plan.Notes)
        {
            var note = TextNote.Create(context.Document, viewId, CreateRevitHelpers.ToXyz(item.Position), item.Text, typeId);
            notes.Add(RevitElementSummary.FromElement(note));
        }

        return ToolResults.Success(new { count = notes.Count, notes }, $"{notes.Count} text note(s) created.");
    }
}
