using LiveTweak.Domain.Models;

namespace LiveTweak.Application.Abstractions;

internal interface ITweakSchemaProvider
{
    Task<IReadOnlyList<TweakEntry>> GetSchemaAsync();
}
