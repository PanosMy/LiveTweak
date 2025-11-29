namespace LiveTweak.Domain.Models;

public sealed record TweakEntry(
    string Id,
    string Name,
    string Label,
    string Category,
    TweakEntryKind Kind,
    SchemaValueKind ValueKind // meaningful only when Kind == Value
);
