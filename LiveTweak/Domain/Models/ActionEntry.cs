using System.Reflection;
using LiveTweak.Domain.Abstractions;

namespace LiveTweak.Domain.Models;

public sealed record ActionEntry(
    string OwnerType,
    string Name,
    string Label,
    string Category,
    MethodInfo? MethodInfo = null
) : SchemaEntry(OwnerType, Name, Label, Category);
