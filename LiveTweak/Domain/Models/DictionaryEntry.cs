using LiveTweak.Domain.Abstractions;

namespace LiveTweak.Domain.Models;

public sealed record DictionaryEntry(
    string OwnerType,
    string Name,
    string Label,
    string Category,
    Type KeyType,
    Type ValueType,
    IReadOnlyList<string> Keys,
    IReadOnlyDictionary<string, string?> Defaults,
    string? Callback
) : SchemaEntry(OwnerType, Name, Label, Category);
