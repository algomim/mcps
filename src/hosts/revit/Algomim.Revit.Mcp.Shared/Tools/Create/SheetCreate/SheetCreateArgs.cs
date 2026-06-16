using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Create.SheetCreate;

internal sealed record SheetCreateArgs(IReadOnlyList<string> Names, IReadOnlyList<long> TitleblockTypeIds)
{
    public static SheetCreateArgs From(ArgumentReader arguments)
        => new(arguments.RequireStringArray("names", 500), arguments.OptionalLongArray("titleblockTypeIds", 500));
}
