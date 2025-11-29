namespace LiveTweak.Domain.Models;

public sealed record TweakCommand(
    TweakCommandType Type,
    string EntryId,
    string? Value = null,
    string? Key = null // For dictionary entries only
);
