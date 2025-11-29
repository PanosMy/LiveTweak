using System.Reflection;
using LiveTweak.Application.dtos;

namespace LiveTweak.Application.Abstractions;

internal interface IAttributeReader
{
    bool HasTweak(MemberInfo member);
    bool HasAction(MemberInfo member);
    ReadTweakDto ReadTweak(MemberInfo member);
    ReadActionDto ReadAction(MethodInfo method);
}
