using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Create.View3DCreate;

internal sealed record View3DCreateArgs(IReadOnlyList<string> Names)
{
    public static View3DCreateArgs From(ArgumentReader arguments)
        => new(arguments.RequireStringArray("names", 100));
}
