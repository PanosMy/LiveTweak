using LiveTweak.Application.Abstractions;
using LiveTweak.Domain.Abstractions;
using LiveTweak.Domain.Models;

namespace LiveTweak.Infrastructure.Reflection;

internal sealed class ReflectionSchemaProvider : ITweakSchemaProvider
{
    private readonly ITweakSource _source;

    internal ReflectionSchemaProvider(ITweakSource source) => _source = source;

    Task<IReadOnlyList<TweakEntry>> ITweakSchemaProvider.GetSchemaAsync()
    {
        return Task.Run(() =>
        {
            var raw = _source.BuildSchema()?.ToArray() ?? [];
            return (IReadOnlyList<TweakEntry>)[.. raw.Select(Map)];
        });
    }

    private static TweakEntry Map(SchemaEntry schema)
    {
        var kind = schema switch
        {
            ValueEntry => TweakEntryKind.Value,
            ActionEntry => TweakEntryKind.Action,
            DictionaryEntry => TweakEntryKind.Dictionary,
            _ => TweakEntryKind.Unknown
        };
        var valueKind = schema is ValueEntry ve ? ve.Kind : SchemaValueKind.Unknown;
        var label = schema.Label ?? schema.Name;
        var cat = schema.Category ?? "General";
        var id = $"{schema.OwnerType ?? "OwnerUnknown"}:{schema.Name}";
        return new TweakEntry(id, schema.Name, label, cat, kind, valueKind);
    }
}
