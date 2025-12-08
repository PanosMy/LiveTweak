using System.Reflection;
using LiveTweak.Application.Abstractions;
using LiveTweak.Application.dtos;

namespace LiveTweak.Infrastructure.Reflection;

internal sealed class AttributeReaderAdapter : IAttributeReader
{
    bool IAttributeReader.HasTweak(MemberInfo member) => AttributeReader.GetCustomAttributeSafe(member, "LiveTweakAttribute") is not null;
    bool IAttributeReader.HasAction(MemberInfo member) => AttributeReader.GetCustomAttributeSafe(member, "LiveTweakActionAttribute") is not null;

    ReadTweakDto IAttributeReader.ReadTweak(MemberInfo member)
    {
        var attr = AttributeReader.GetCustomAttributeSafe(member, "LiveTweakAttribute");
        string label = member.Name;
        double min = double.NaN;
        double max = double.NaN;
        string? category = null;
        string? callback = null;
        if (attr != null)
            AttributeReader.ReadTweak(attr, out label, out min, out max, out category, out callback);

        return new(
            Label: label,
            Min: min,
            Max: max,
            Category: category,
            Callback: callback);
    }

    ReadActionDto IAttributeReader.ReadAction(MethodInfo method)
    {
        var attr = AttributeReader.GetCustomAttributeSafe(method, "LiveTweakActionAttribute");
        string label = method.Name;
        string? category = null;

        if (attr != null)
            AttributeReader.ReadAction(attr, out label, out category);

        return new(
            Label: label,
            Category: category);
    }
}
