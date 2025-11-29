namespace LiveTweak.Domain.Models;

public sealed record TweakCommandResult(
    bool Ok,
    string Message,
    string? NewValue = null
);
