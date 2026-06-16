using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Create.TextNotesCreate;

internal sealed record TextNotesCreateArgs(
    long ViewId,
    IReadOnlyList<string> Texts,
    IReadOnlyList<double> X,
    IReadOnlyList<double> Y,
    IReadOnlyList<double> Z)
{
    public static TextNotesCreateArgs From(ArgumentReader arguments)
        => new(
            arguments.RequireLong("viewId"),
            arguments.RequireStringArray("texts", 500),
            arguments.RequireDoubleArray("x", 500),
            arguments.RequireDoubleArray("y", 500),
            arguments.RequireDoubleArray("z", 500));
}
