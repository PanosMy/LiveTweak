using LiveTweak.Domain.Abstractions;

namespace LiveTweak.Application.Abstractions;

internal interface ITweakSource
{
    IReadOnlyList<SchemaEntry> BuildSchema();
}
