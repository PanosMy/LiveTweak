namespace LiveTweak.Domain.Abstractions;

public abstract record SchemaEntry(
    string OwnerType,
    string Name,
    string Label,
    string Category
);
