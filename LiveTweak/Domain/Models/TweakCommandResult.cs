namespace LiveTweak.Domain.Models;

public sealed record TweakCommandResult<T>(
    bool Ok,
    string Message,
    T? NewValue = default
);

public sealed record TweakCommandResult(
    bool Ok,
    string Message
);
