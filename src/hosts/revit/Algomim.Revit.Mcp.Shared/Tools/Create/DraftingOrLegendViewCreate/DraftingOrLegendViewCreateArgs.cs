using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Create.DraftingOrLegendViewCreate;

internal sealed record DraftingOrLegendViewCreateArgs(IReadOnlyList<string> Names, IReadOnlyList<bool> DraftingViews)
{
    public static DraftingOrLegendViewCreateArgs From(ArgumentReader arguments)
        => new(arguments.RequireStringArray("names", 100), arguments.RequireBoolArray("draftingViews", 100));
}
