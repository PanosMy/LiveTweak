using System.Reflection;
using LiveTweak.Application.Abstractions;
using LiveTweak.Domain.Abstractions;
using LiveTweak.Domain.Models;
using Convert = LiveTweak.Infrastructure.Helper.Convert;

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

    Task<TweakCommandResult> ITweakCommandDispatcher.DispatchAsync(TweakCommand command)
    {
        EnsureIndex();
        if (!_index.TryGetValue(command.EntryId, out var entry))
            return Task.FromResult(new TweakCommandResult(false, "Entry not found"));

        return command.Type switch
        {
            TweakCommandType.SetValue => HandleSet((ValueEntry)entry, command.Value),
            TweakCommandType.RevertValue => HandleRevert((ValueEntry)entry),
            TweakCommandType.InvokeAction => HandleInvoke((ActionEntry)entry),
            TweakCommandType.SetDictionaryValue => HandleDictSet((DictionaryEntry)entry, command.Value, command.Key!),
            TweakCommandType.RevertDictionaryValue => HandleDictRevert((DictionaryEntry)entry, command.Key!),
            _ => Task.FromResult(new TweakCommandResult(false, "Unsupported command type"))
        };
    }

    private Task<TweakCommandResult> HandleDictSet(DictionaryEntry entry, string? value, string key)
    {
        var ok = DictionaryReflection.TrySetStaticValue(entry, key, value, out var err);
        if (!ok)
            return Task.FromResult(new TweakCommandResult(false, err ?? "Set failed"));

        var keyWithType = Convert.ChangeType(key, entry.KeyType);
        var valueWithType = Convert.ChangeType(value, entry.ValueType);
        TryInvokeOnChanged(entry.OwnerType, entry.Callback, entry.Label, keyWithType, valueWithType);

        var current = DictionaryReflection.TryGetCurrentOrDefault(entry, key);

        return Task.FromResult(new TweakCommandResult(true, "Set ok", current));
    }

    private Task<TweakCommandResult> HandleDictRevert(DictionaryEntry entry, string key)
    {
        var def = DictionaryReflection.TryGetDefault(entry, key);
        var ok = DictionaryReflection.TrySetStaticValue(entry, key, def, out var err);
        if (!ok)
            return Task.FromResult(new TweakCommandResult(false, err ?? "Revert failed"));

        var keyWithType = Convert.ChangeType(key, entry.KeyType);
        var valueWithType = Convert.ChangeType(def, entry.ValueType);
        TryInvokeOnChanged(entry.OwnerType, entry.Callback, entry.Label, keyWithType, valueWithType);

        return Task.FromResult(new TweakCommandResult(true, "Reverted", def));
    }

    private Task<TweakCommandResult> HandleSet(ValueEntry valueEntry, string? value)
    {
        var ok = ValueReflection.TrySetStaticValue(valueEntry, value, out var err);
        if (!ok)
            return Task.FromResult(new TweakCommandResult(false, err ?? "Set failed"));


        TryInvokeOnChanged(valueEntry.OwnerType, valueEntry.Callback, valueEntry.Label);
        var current = ValueReflection.TryGetCurrentOrDefault(valueEntry);

        return Task.FromResult(new TweakCommandResult(true, "Set ok", current));
    }

    private Task<TweakCommandResult> HandleRevert(ValueEntry valueEntry)
    {
        var def = ValueReflection.TryGetDefault(valueEntry);
        var ok = ValueReflection.TrySetStaticValue(valueEntry, def, out var err);
        if (!ok)
            return Task.FromResult(new TweakCommandResult(false, err ?? "Revert failed"));

        TryInvokeOnChanged(valueEntry.OwnerType, valueEntry.Callback, valueEntry.Label);

        return Task.FromResult(new TweakCommandResult(true, "Reverted", def));
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

    public static void TryInvokeOnChanged(
        string ownerTypeName,
        string? methodName,
        string member,
        object? value = null,
        object? key = null)
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
                if (parameters.Length == 3 &&
                    parameters[0].ParameterType == typeof(string) &&
                    parameters[1].ParameterType.IsAssignableFrom(key?.GetType() ?? typeof(object)) &&
                    parameters[2].ParameterType.IsAssignableFrom(value?.GetType() ?? typeof(object)))
                {
                    method.Invoke(null, [member, key, value]);
                    return;
                }
                else if (parameters.Length == 2 &&
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
