using System.Collections;
using System.Reflection;
using LiveTweak.Application.Abstractions;
using LiveTweak.Domain.Abstractions;
using LiveTweak.Domain.Models;
using DictEntry = LiveTweak.Domain.Models.DictionaryEntry;

namespace LiveTweak.Infrastructure.Reflection;

internal sealed class ReflectionTweakSource : ITweakSource
{
    private readonly IAttributeReader _attributes;

    internal ReflectionTweakSource(IAttributeReader attributes)
    {
        _attributes = attributes;
    }

    IReadOnlyList<SchemaEntry> ITweakSource.BuildSchema()
    {
        var result = new List<SchemaEntry>();
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try
            { types = asm.GetTypes(); }
            catch { continue; }

            foreach (var t in types)
            {
                if (!t.IsClass)
                    continue;

                if (!HasAnyTweakOrAction(t))
                    continue;

                result.AddRange(BuildEntries(t));
            }
        }
        return result;
    }

    private bool HasAnyTweakOrAction(Type type)
    {
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        try
        {
            if (type.GetFields(Flags).Any(f => _attributes.HasTweak(f)))
                return true;

            if (type.GetProperties(Flags).Any(p => _attributes.HasTweak(p)))
                return true;

            if (type.GetMethods(Flags).Any(m => _attributes.HasAction(m)))
                return true;
        }
        catch
        {
        }
        return false;
    }

    private IEnumerable<SchemaEntry> BuildEntries(Type owner)
    {
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        foreach (var f in owner.GetFields(Flags))
        {
            if (!_attributes.HasTweak(f))
                continue;

            if (IsDictionaryType(f.FieldType))
            {
                yield return BuildDict(owner, f);
            }
            else
            {
                yield return BuildValue(owner, f, isField: true);
            }
        }

        foreach (var p in owner.GetProperties(Flags))
        {
            if (!_attributes.HasTweak(p))
                continue;

            if (IsDictionaryType(p.PropertyType))
            {
                yield return BuildDict(owner, p);
            }
            else
            {
                yield return BuildValue(owner, p, isField: false);
            }
        }

        foreach (var m in owner.GetMethods(Flags))
        {
            if (!_attributes.HasAction(m))
                continue;

            var (Label, Category) = _attributes.ReadAction(m);
            yield return new ActionEntry(owner.FullName ?? owner.Name, m.Name, Label ?? m.Name, Category ?? "General", m);
        }
    }

    private ValueEntry BuildValue(Type owner, MemberInfo member, bool isField)
    {
        var (label, min, max, category, callback) = _attributes.ReadTweak(member);
        var type = isField ? ((FieldInfo)member).FieldType : ((PropertyInfo)member).PropertyType;
        var kind = ToKind(type);
        object? current = null;
        try
        {
            if (isField)
            {
                var f = (FieldInfo)member;
                if (f.IsStatic && !f.IsLiteral)
                    current = f.GetValue(null);
            }
            else
            {
                var p = (PropertyInfo)member;
                var gm = p.GetGetMethod(true);
                if (gm?.IsStatic == true)
                    current = p.GetValue(null);
            }
        }
        catch
        {
        }

        return new ValueEntry(
            owner.FullName ?? owner.Name,
            member.Name,
            label ?? member.Name,
            category ?? "General",
            kind,
            min,
            max,
            ToSerializable(current),
            callback,
            type.IsEnum ? type.FullName : null
        );
    }

    private DictEntry BuildDict(Type owner, MemberInfo member)
    {
        var (label, _, _, category, callback) = _attributes.ReadTweak(member);
        var memberType = member is FieldInfo fi ? fi.FieldType : ((PropertyInfo)member).PropertyType;

        Type keyType = typeof(string);
        Type valueType = typeof(string);
        if (TryGetGenericDictionaryKV(memberType, out var k, out var v))
        {
            keyType = k;
            valueType = v;
        }

        object? dictObj = null;
        try
        {
            if (member is FieldInfo f && f.IsStatic)
            {
                dictObj = f.GetValue(null);
            }
            else if (member is PropertyInfo p)
            {
                var gm = p.GetGetMethod(true);

                if (gm?.IsStatic == true)
                    dictObj = p.GetValue(null);
            }
        }
        catch
        {
        }

        var keys = new List<string>();
        var defaults = new Dictionary<string, string?>();

        if (dictObj is IDictionary nong)
        {
            foreach (var k2 in nong.Keys)
            {
                var s = k2?.ToString() ?? string.Empty;
                keys.Add(s);
                try
                { defaults[s] = ToSerializable(nong[k2!])?.ToString(); }
                catch
                {
                }
            }
        }
        else if (dictObj != null && TryGetGenericDictionaryKV(dictObj.GetType(), out var kT, out var vT))
        {
            var kvpType = typeof(KeyValuePair<,>).MakeGenericType(kT, vT);
            foreach (var item in (IEnumerable)dictObj)
            {
                var keyProp = kvpType.GetProperty("Key");
                var valProp = kvpType.GetProperty("Value");
                var ks = keyProp!.GetValue(item)?.ToString() ?? string.Empty;
                var vv = valProp!.GetValue(item);
                keys.Add(ks);
                defaults[ks] = ToSerializable(vv)?.ToString();
            }
        }

        return new DictEntry(
            owner.FullName ?? owner.Name,
            member.Name,
            label ?? member.Name,
            category ?? "General",
            keyType,
            valueType,
            keys,
            defaults,
            callback
        );
    }

    private static SchemaValueKind ToKind(Type t)
    {
        if (IsDictionaryType(t))
            return SchemaValueKind.Dictionary;

        if (t == typeof(bool))
            return SchemaValueKind.Boolean;

        if (t == typeof(int))
            return SchemaValueKind.Integer;

        if (t == typeof(float))
            return SchemaValueKind.Float;

        if (t == typeof(double))
            return SchemaValueKind.Double;

        if (t == typeof(string))
            return SchemaValueKind.String;

        if (t.IsEnum)
            return SchemaValueKind.Enum;

        return SchemaValueKind.String;
    }

    private static bool IsDictionaryType(Type t)
    {
        if (typeof(IDictionary).IsAssignableFrom(t))
            return true;

        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            return true;

        return t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
    }

    private static bool TryGetGenericDictionaryKV(Type t, out Type keyType, out Type valueType)
    {
        keyType = null!;
        valueType = null!;
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>))
        {
            var args = t.GetGenericArguments();
            keyType = args[0];
            valueType = args[1];

            return true;
        }
        var iface = t.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        if (iface != null)
        {
            var args = iface.GetGenericArguments();
            keyType = args[0];
            valueType = args[1];
            return true;
        }

        return false;
    }

    private static object? ToSerializable(object? value)
    {
        if (value is null)
            return null;

        var t = value.GetType();
        if (t.IsEnum)
            return value.ToString();

        if (IsSimple(t))
            return value;

        return t.FullName ?? t.Name;
    }

    private static bool IsSimple(Type t)
    {
        if (t.IsPrimitive)
            return true;

        return t == typeof(string)
               || t == typeof(decimal)
               || t == typeof(DateTime)
               || t == typeof(DateTimeOffset)
               || t == typeof(TimeSpan)
               || t == typeof(Guid)
               || t == typeof(float)
               || t == typeof(double)
               || t == typeof(bool)
               || t == typeof(byte)
               || t == typeof(sbyte)
               || t == typeof(short)
               || t == typeof(ushort)
               || t == typeof(int)
               || t == typeof(uint)
               || t == typeof(long)
               || t == typeof(ulong);
    }
}

