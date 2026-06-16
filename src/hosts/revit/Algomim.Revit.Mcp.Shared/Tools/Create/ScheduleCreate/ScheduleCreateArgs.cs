using Algomim.Revit.Mcp.Tools.Common;

namespace Algomim.Revit.Mcp.Tools.Create.ScheduleCreate;

internal sealed record ScheduleCreateArgs(string Name, long CategoryId, IReadOnlyList<long> ParameterIds)
{
    public static ScheduleCreateArgs From(ArgumentReader arguments)
        => new(
            arguments.RequireString("name"),
            arguments.RequireLong("categoryId"),
            arguments.RequireLongArray("parameterIds", 100));
}
