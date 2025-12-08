using LiveTweak.Domain.Abstractions;

namespace LiveTweak.Domain.Models;

public sealed record ValueEntry(
    string OwnerType,
    string Name,
    string Label,
    string Category,
    SchemaValueKind Kind,
    double Min,
    double Max,
    object? DefaultValue,
    string? Callback,
    string? EnumType
) : SchemaEntry(OwnerType, Name, Label, Category);
