using System.Collections;
using System.Reflection;
using LiveTweak.Application.Abstractions;
using LiveTweak.Domain.Abstractions;
using LiveTweak.Domain.Models;
using LiveTweak.Infrastructure.Services;
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
                yield return BuildDictionary(owner, f);
            }
            else if (IsCollectionType(f.FieldType))
            {
                yield return BuildCollection(owner, f);
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
                yield return BuildDictionary(owner, p);
            }
            else if (IsCollectionType(p.PropertyType))
            {
                yield return BuildCollection(owner, p);
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

    private DictEntry BuildDictionary(Type owner, MemberInfo member)
    {
        var (label, min, max, category, callback) = _attributes.ReadTweak(member);
        var memberType = member is FieldInfo fi ? fi.FieldType : ((PropertyInfo)member).PropertyType;

        Type keyType = typeof(string);
        Type valueType = typeof(string);
        if (DictionaryReflection.TryGetGenericDictionaryKV(memberType, out var k, out var v))
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

        var keys = new List<object>();
        var defaults = new Dictionary<object, object?>();

        if (dictObj is IDictionary nong)
        {
            foreach (var kay in nong.Keys)
            {
                keys.Add(kay);
                try
                {
                    defaults[kay] = nong[kay];
                }
                catch
                {
                }
            }
        }
        else if (dictObj != null && DictionaryReflection.TryGetGenericDictionaryKV(dictObj.GetType(), out var kT, out var vT))
        {
            var kvpType = typeof(KeyValuePair).MakeGenericType(kT, vT);
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

        if (min < 0)
            min = 0;

        return new DictEntry(
            OwnerType: owner.FullName ?? owner.Name,
            Name: member.Name,
            Label: label ?? member.Name,
            Category: category ?? "General",
            Max: max,
            Min: min,
            KeyType: keyType,
            ValueType: valueType,
            Keys: keys,
            Defaults: defaults,
            Callback: callback
        );
    }

    private CollectionEntry BuildCollection(Type owner, MemberInfo member)
    {
        var (label, min, max, category, callback) = _attributes.ReadTweak(member);
        var memberType = member is FieldInfo fi ? fi.FieldType : ((PropertyInfo)member).PropertyType;
        Type elementType = typeof(string);

        if (memberType.IsArray)
        {
            elementType = memberType.GetElementType() ?? typeof(string);
        }
        else if (memberType.IsGenericType && memberType.GetGenericArguments().Length == 1)
        {
            elementType = memberType.GetGenericArguments()[0];
        }

        object? collObj = null;
        try
        {
            if (member is FieldInfo f && f.IsStatic)
            {
                collObj = f.GetValue(null);
            }
            else if (member is PropertyInfo p)
            {
                var gm = p.GetGetMethod(true);

                if (gm?.IsStatic == true)
                    collObj = p.GetValue(null);
            }
        }
        catch
        {
        }

        if (collObj != null && memberType.IsArray && double.IsNaN(max))
        {
            var arr = (Array)collObj;
            max = arr.Length;
        }

        var defaults = new List<string>();

        if (collObj is ICollection nong)
        {
            foreach (var item in nong)
            {
                try
                {
                    defaults.Add(ToSerializable(item)?.ToString() ?? string.Empty);
                }
                catch
                {
                }
            }
        }

        if (min < 0)
            min = 0;

        return new CollectionEntry(
            OwnerType: owner.FullName ?? owner.Name,
            Name: member.Name,
            Label: label ?? member.Name,
            Category: category ?? "General",
            Max: max,
            Min: min,
            Defaults: defaults,
            ElementType: elementType,
            Callback: callback
        );
    }

    private static SchemaValueKind ToKind(Type t)
    {
        if (t.IsEnum)
            return SchemaValueKind.Enum;

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

        if (IsDictionaryType(t))
            return SchemaValueKind.Dictionary;

        if (IsCollectionType(t))
            return SchemaValueKind.Collection;

        return SchemaValueKind.String;
    }

    private static bool IsDictionaryType(Type t)
    {
        if (typeof(IDictionary).IsAssignableFrom(t))
            return true;

        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary))
            return true;

        if (t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary)))
            return true;

        return false;
    }

    private static bool IsCollectionType(Type t)
    {
        if (typeof(ICollection).IsAssignableFrom(t))
            return true;

        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection))
            return true;

        if (t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection)))
            return true;

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

