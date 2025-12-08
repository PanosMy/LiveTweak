using System.Collections;
using System.Reflection;
using LiveTweak.Application.Abstractions;
using LiveTweak.Domain.Abstractions;
using LiveTweak.Domain.Models;
using LiveTweak.Infrastructure.Services;
using dictEntry = LiveTweak.Domain.Models.DictionaryEntry;
namespace LiveTweak.Infrastructure.Reflection;

internal sealed class ReflectionCommandDispatcher : ITweakCommandDispatcher
{
    private readonly ITweakSource _source;
    private readonly Dictionary<string, SchemaEntry> _index = [];

    internal ReflectionCommandDispatcher(ITweakSource source) => _source = source;

    private void EnsureIndex()
    {
        if (_index.Count > 0)
            return;

        var entries = _source.BuildSchema() ?? [];
        _index.Clear();

        foreach (var e in entries)
        {
            var id = $"{e.OwnerType ?? "OwnerUnknown"}:{e.Name}";
            _index[id] = e;
        }
    }

    Task<TweakCommandResult<T>> ITweakCommandDispatcher.DispatchAsync<T>(TweakCommand<T> command)
    {
        EnsureIndex();
        if (!_index.TryGetValue(command.EntryId, out var entry))
            return Task.FromResult(new TweakCommandResult<T>(false, "Entry not found"));

        object result = command.Type switch
        {
            TweakCommandType.SetValue => HandleSet((ValueEntry)entry, command.Value?.ToString()),
            TweakCommandType.RevertValue => HandleRevert((ValueEntry)entry),
            TweakCommandType.InvokeAction => HandleInvoke((ActionEntry)entry),
            TweakCommandType.SetDictionaryValue => HandleDictSet((dictEntry)entry, command.Value as IDictionary),
            TweakCommandType.RevertDictionaryValue => HandleDictRevert((dictEntry)entry, command.Key),
            TweakCommandType.SetCollectionValue => HandleCollectionSet((CollectionEntry)entry, command.Value as ICollection),
            TweakCommandType.RevertCollectionValue => HandleCollectionRevert((CollectionEntry)entry),
            _ => Task.FromResult(new TweakCommandResult<T>(false, "Unsupported command type"))
        };

        return (Task<TweakCommandResult<T>>)result;
    }

    private Task<TweakCommandResult<ICollection>> HandleCollectionSet(CollectionEntry entry, ICollection? value)
    {
        var ok = CollectionReflection.TrySetStaticValue(entry, value, out var err);
        if (!ok)
            return Task.FromResult(new TweakCommandResult<ICollection>(false, err ?? "Set failed"));

        TryInvokeOnChanged(entry.OwnerType, entry.Callback, entry.Label, value);

        var current = CollectionReflection.TryGetCurrentOrDefault(entry);

        return Task.FromResult(new TweakCommandResult<ICollection>(true, "Set ok", current));
    }

    private Task<TweakCommandResult<ICollection>> HandleCollectionRevert(CollectionEntry entry)
    {
        var def = entry.Defaults;
        var ok = CollectionReflection.TrySetStaticValue(entry, def, out var err);
        if (!ok)
            return Task.FromResult(new TweakCommandResult<ICollection>(false, err ?? "Revert failed"));

        TryInvokeOnChanged(entry.OwnerType, entry.Callback, entry.Label, def);

        return Task.FromResult(new TweakCommandResult<ICollection>(true, "Reverted", def));
    }

    private Task<TweakCommandResult<IDictionary>> HandleDictSet(dictEntry entry, IDictionary? value)
    {
        var ok = DictionaryReflection.TrySetStaticValue(entry, value, out var err);
        if (!ok)
            return Task.FromResult(new TweakCommandResult<IDictionary>(false, err ?? "Set failed"));

        TryInvokeOnChanged(entry.OwnerType, entry.Callback, entry.Label, value);

        var current = DictionaryReflection.TryGetCurrentOrDefault(entry);

        return Task.FromResult(new TweakCommandResult<IDictionary>(true, "Set ok", current));
    }

    private Task<TweakCommandResult<IDictionary>> HandleDictRevert(dictEntry entry, object? key)
    {
        IDictionary newDictionary;
        if (key == null)
            newDictionary = entry.Defaults;
        else
        {
            var currentDict = DictionaryReflection.TryGetCurrentOrDefault(entry);
            var defaultDict = entry.Defaults;
            var defValue = defaultDict[key?.ToString() ?? string.Empty];
            var keyWithType = Helper.Convert.ChangeType(key, entry.KeyType);
            var valueWithType = Helper.Convert.ChangeType(defValue, entry.ValueType);
            currentDict[keyWithType] = valueWithType;
            newDictionary = currentDict;
        }

        var ok = DictionaryReflection.TrySetStaticValue(entry, newDictionary, out var err);
        if (!ok)
            return Task.FromResult(new TweakCommandResult<IDictionary>(false, err ?? "Revert failed"));

        TryInvokeOnChanged(entry.OwnerType, entry.Callback, entry.Label, newDictionary);

        return Task.FromResult(new TweakCommandResult<IDictionary>(true, "Reverted", newDictionary));
    }

    private Task<TweakCommandResult<string>> HandleSet(ValueEntry valueEntry, string? value)
    {
        var ok = ValueReflection.TrySetStaticValue(valueEntry, value, out var err);
        if (!ok)
            return Task.FromResult(new TweakCommandResult<string>(false, err ?? "Set failed"));


        TryInvokeOnChanged(valueEntry.OwnerType, valueEntry.Callback, valueEntry.Label, value);
        var current = ValueReflection.TryGetCurrentOrDefault(valueEntry);

        return Task.FromResult(new TweakCommandResult<string>(true, "Set ok", current));
    }

    private Task<TweakCommandResult<string>> HandleRevert(ValueEntry valueEntry)
    {
        var def = ValueReflection.TryGetDefault(valueEntry);
        var ok = ValueReflection.TrySetStaticValue(valueEntry, def, out var err);
        if (!ok)
            return Task.FromResult(new TweakCommandResult<string>(false, err ?? "Revert failed"));

        TryInvokeOnChanged(valueEntry.OwnerType, valueEntry.Callback, valueEntry.Label, def);

        return Task.FromResult(new TweakCommandResult<string>(true, "Reverted", def));
    }

    private Task<TweakCommandResult> HandleInvoke(ActionEntry a)
    {
        try
        {
            var mi = a.MethodInfo;
            if (mi is null)
                return Task.FromResult(new TweakCommandResult(false, "MethodInfo missing"));

            object? instance = mi.IsStatic ? null : ReflectionUtils.ResolveOwnerInstance(mi.DeclaringType!);
            mi.Invoke(instance, null);
            return Task.FromResult(new TweakCommandResult(true, "Invoked"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TweakCommandResult(false, ex.GetBaseException().Message));
        }
    }

    public static void TryInvokeOnChanged<T>(
        string ownerTypeName,
        string? methodName,
        string member,
        T? value)
    {
        if (string.IsNullOrEmpty(ownerTypeName) || string.IsNullOrEmpty(methodName))
            return;

        var ownerType = ReflectionUtils.ResolveType(ownerTypeName);
        if (ownerType is null)
            return;

        var methods = ownerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => m.Name == methodName)
            .ToArray();

        if (methods.Length == 0)
            return;

        foreach (var method in methods.OrderByDescending(m => m.GetParameters().Length))
        {
            var parameters = method.GetParameters();
            try
            {
                if (parameters.Length == 2 &&
                   parameters[0].ParameterType == typeof(string) &&
                   parameters[1].ParameterType.IsAssignableFrom(value?.GetType() ?? typeof(object)))
                {
                    method.Invoke(null, [member, value]);
                    return;
                }
                else if (parameters.Length == 1 &&
                    parameters[0].ParameterType.IsAssignableFrom(value?.GetType() ?? typeof(object)))
                {
                    method.Invoke(null, [value]);
                    return;
                }
                else if (parameters.Length == 0)
                {
                    method.Invoke(null, null);
                    return;
                }
            }
            catch
            {
            }
        }
    }
}
