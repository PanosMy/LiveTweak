namespace LiveTweak.Domain.Models;

public sealed record TweakCommand<T>(
    TweakCommandType Type,
    string EntryId,
    T? Value = default,
    object? Key = default
);
public sealed record TweakCommand(
    TweakCommandType Type,
    string EntryId
);
