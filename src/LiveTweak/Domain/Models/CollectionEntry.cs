using System.Collections;
using LiveTweak.Domain.Abstractions;

namespace LiveTweak.Domain.Models;

public sealed record CollectionEntry(
    string OwnerType,
    string Name,
    string Label,
    string Category,
    double Max,
    double Min,
    Type ElementType,
    ICollection Defaults,
    string? Callback
) : SchemaEntry(OwnerType, Name, Label, Category);
