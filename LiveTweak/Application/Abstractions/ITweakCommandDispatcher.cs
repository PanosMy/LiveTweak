using LiveTweak.Domain.Models;

namespace LiveTweak.Application.Abstractions;

internal interface ITweakCommandDispatcher
{
    Task<TweakCommandResult> DispatchAsync(TweakCommand command);
}
