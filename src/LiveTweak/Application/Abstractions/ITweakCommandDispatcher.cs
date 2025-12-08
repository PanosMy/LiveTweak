using LiveTweak.Domain.Models;

namespace LiveTweak.Application.Abstractions;

internal interface ITweakCommandDispatcher
{
    Task<TweakCommandResult<T>> DispatchAsync<T>(TweakCommand<T> command);
}
