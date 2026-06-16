using System.Linq;
using System.Reflection;

namespace Algomim.Revit.Mcp.Discovery;

/// <summary>
/// Reflects a Revit API type into a curated, token-efficient summary: non-obsolete public members,
/// truncated, with short signatures. Obsolete members are dropped to steer the agent away from
/// deprecated APIs (same intent as the script pre-validator).
/// </summary>
internal static class RevitTypeInspector
{
    private const int MaxMethods = 40;
    private const int MaxProperties = 40;
    private const int MaxNested = 15;

    public static object InspectType(Type type, string? memberName)
    {
        if (type.IsEnum)
            return new { typeName = type.FullName, kind = "enum", values = Enum.GetNames(type) };

        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        bool NotObsolete(MemberInfo m) => m.GetCustomAttribute<ObsoleteAttribute>() is null;
        bool NameMatches(string name) => string.IsNullOrEmpty(memberName) ||
            name.IndexOf(memberName!, StringComparison.OrdinalIgnoreCase) >= 0;

        var methods = type.GetMethods(flags)
            .Where(mi => !mi.IsSpecialName && NotObsolete(mi) && NameMatches(mi.Name))
            .Take(MaxMethods)
            .Select(FormatMethod)
            .ToList();

        var properties = type.GetProperties(flags)
            .Where(pi => NotObsolete(pi) && NameMatches(pi.Name))
            .Take(MaxProperties)
            .Select(pi => new
            {
                name = pi.Name,
                type = FormatTypeName(pi.PropertyType),
                canRead = pi.CanRead,
                canWrite = pi.CanWrite,
                isStatic = pi.GetMethod?.IsStatic ?? false,
            })
            .ToList();

        var result = new Dictionary<string, object>
        {
            ["typeName"] = type.FullName ?? type.Name,
            ["kind"] = type.IsInterface ? "interface" : type.IsAbstract ? "abstract class" : "class",
            ["methods"] = methods,
            ["properties"] = properties,
        };

        var nested = type.GetNestedTypes(BindingFlags.Public)
            .Take(MaxNested)
            .Select(nt => new
            {
                name = nt.Name,
                fullName = $"{type.Name}.{nt.Name}",
                kind = nt.IsEnum ? "enum" : nt.IsAbstract ? "abstract class" : "class",
                members = nt.GetProperties(BindingFlags.Public | BindingFlags.Static).Take(10).Select(p => p.Name).ToList(),
            })
            .ToList();

        if (nested.Count > 0) result["nestedTypes"] = nested;
        if (type.BaseType != null && type.BaseType != typeof(object)) result["baseClass"] = type.BaseType.Name;

        return result;
    }

    private static object FormatMethod(MethodInfo mi)
    {
        var parameters = mi.GetParameters()
            .Select(p => $"{FormatTypeName(p.ParameterType)} {p.Name}" +
                (p.HasDefaultValue ? $" = {p.DefaultValue ?? "null"}" : ""));

        return new
        {
            name = mi.Name,
            returnType = FormatTypeName(mi.ReturnType),
            parameters = string.Join(", ", parameters),
            isStatic = mi.IsStatic,
        };
    }

    private static string FormatTypeName(Type t)
    {
        if (t == typeof(void)) return "void";
        if (t == typeof(string)) return "string";
        if (t == typeof(int)) return "int";
        if (t == typeof(long)) return "long";
        if (t == typeof(double)) return "double";
        if (t == typeof(bool)) return "bool";
        if (t == typeof(object)) return "object";

        if (t.IsGenericType)
        {
            var baseName = t.Name.Split('`')[0];
            var args = string.Join(", ", t.GetGenericArguments().Select(FormatTypeName));
            return $"{baseName}<{args}>";
        }

        return t.Name;
    }
}
