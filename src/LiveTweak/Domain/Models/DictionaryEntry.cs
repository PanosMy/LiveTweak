using System.Collections;
using LiveTweak.Domain.Abstractions;

namespace LiveTweak.Domain.Models;

public sealed record DictionaryEntry(
    string OwnerType,
    string Name,
    string Label,
    string Category,
    double Max,
    double Min,
    Type KeyType,
    Type ValueType,
    IReadOnlyList<object> Keys,
    IDictionary Defaults,
    string? Callback
) : SchemaEntry(OwnerType, Name, Label, Category);
