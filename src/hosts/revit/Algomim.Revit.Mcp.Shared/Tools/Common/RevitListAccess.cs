using Autodesk.Revit.DB;

namespace Algomim.Revit.Mcp.Tools.Common;

internal static class RevitListAccess
{
    public static XYZ VectorAt(IReadOnlyList<double> x, IReadOnlyList<double> y, IReadOnlyList<double> z, int index)
        => new(ValueAt(x, index), ValueAt(y, index), ValueAt(z, index));

    public static Line AxisAt(
        IReadOnlyList<double> startX,
        IReadOnlyList<double> startY,
        IReadOnlyList<double> startZ,
        IReadOnlyList<double> endX,
        IReadOnlyList<double> endY,
        IReadOnlyList<double> endZ,
        int index)
        => Line.CreateBound(
            new XYZ(ValueAt(startX, index), ValueAt(startY, index), ValueAt(startZ, index)),
            new XYZ(ValueAt(endX, index), ValueAt(endY, index), ValueAt(endZ, index)));

    public static T ValueAt<T>(IReadOnlyList<T> values, int index)
    {
        if (values.Count == 0)
            throw new ToolArgumentException("values", "list cannot be empty");

        return values.Count == 1 ? values[0] : values[index];
    }

    public static void EnsureCompatibleLengths(string name, int itemCount, params IReadOnlyList<object>[] lists)
    {
        foreach (var list in lists)
        {
            if (list.Count is 1 || list.Count == itemCount) continue;
            throw new ToolArgumentException(name, "lists must have either one value or the same length as ids");
        }
    }
}

