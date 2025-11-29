using System.Reflection;

namespace LiveTweak.Infrastructure.Reflection;

internal static class AttributeReader
{
    internal static Attribute GetCustomAttributeSafe(MemberInfo member, string attributeTypeName)
    {
        if (member == null || string.IsNullOrWhiteSpace(attributeTypeName))
            return null;

        try
        {
            foreach (var attr in member.GetCustomAttributes(inherit: true))
            {
                var t = attr.GetType();
                if (t.Name.Equals(attributeTypeName, StringComparison.Ordinal)
                    || (t.FullName?.EndsWith("." + attributeTypeName, StringComparison.Ordinal) ?? false))
                {
                    return (Attribute)attr;
                }
            }
        }
        catch { }
        return null;
    }

    internal static void ReadTweak(
        Attribute attr,
        out string? label,
        out double min,
        out double max,
        out string? category,
        out string? callback)
    {
        label = null;
        min = double.NaN;
        max = double.NaN;
        category = null;
        callback = null;
        var t = attr?.GetType();
        if (t == null)
            return;
        try
        {
            label = (string?)t.GetProperty("Label")?.GetValue(attr);
        }
        catch { }

        try
        {
            min = (double?)t.GetProperty("Min")?.GetValue(attr) ?? double.NaN;
        }
        catch { }

        try
        {
            max = (double?)t.GetProperty("Max")?.GetValue(attr) ?? double.NaN;
        }
        catch { }

        try
        {
            category = (string?)t.GetProperty("Category")?.GetValue(attr);
        }
        catch
        {
        }

        try
        {
            callback = (string?)t.GetProperty("OnChanged")?.GetValue(attr);
        }
        catch
        {
        }
    }

    internal static void ReadAction(Attribute attr, out string? label, out string? category)
    {
        label = null;
        category = null;
        var t = attr?.GetType();

        if (t == null)
            return;

        try
        {
            label = (string)t.GetProperty("Label")?.GetValue(attr);
        }
        catch
        {
        }

        try
        {
            category = (string)t.GetProperty("Category")?.GetValue(attr);
        }
        catch
        {
        }
    }
}
