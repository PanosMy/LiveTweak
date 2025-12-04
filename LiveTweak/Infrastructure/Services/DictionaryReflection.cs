using System.Collections;
using System.Reflection;
using LiveTweak.Infrastructure.Helper;
using dictEntry = LiveTweak.Domain.Models.DictionaryEntry;

namespace LiveTweak.Infrastructure.Services;

internal static class DictionaryReflection
{
    public static IDictionary TryGetCurrentOrDefault(dictEntry dict)
    {
        var current = TryGetCurrent(dict);
        if (current != null)
            return current;

        return dict.Defaults;
    }


    private static IDictionary? TryGetCurrent(dictEntry dict)
    {
        var ownerType = ResolveOwnerType(dict.OwnerType);
        if (ownerType == null)
        {
            return null;
        }

        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        var member = (MemberInfo?)ownerType.GetField(dict.Name, flags)
                            ?? ownerType.GetProperty(dict.Name, flags);
        if (member == null)
        {
            return null;
        }

        object? dictObj =
            member is FieldInfo fi ? fi.GetValue(null) :
            member is PropertyInfo pi ? pi.GetValue(null) :
            null;

        if (dictObj == null)
        {

            return null;
        }

        if (dictObj is IDictionary currDict)
        {
            return currDict;
        }

        return null;
    }

    public static bool TrySetStaticValue(dictEntry dictionary, IDictionary? value, out string? error)
    {
        error = null;
        try
        {
            var ownerType = ResolveOwnerType(dictionary.OwnerType);
            if (ownerType == null)
            {
                error = "Owner type not found";
                return false;
            }

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            var member = (MemberInfo?)ownerType.GetField(dictionary.Name, flags)
                                 ?? ownerType.GetProperty(dictionary.Name, flags);

            if (member == null)
            {
                error = "Dictionary member not found";
                return false;
            }

            object? dictObj =
                member is FieldInfo fi ? fi.GetValue(null) :
                member is PropertyInfo pi ? pi.GetValue(null) :
                null;


            if (dictObj is not IDictionary)
            {
                error = "Target is not a dictionary";
                return false;
            }


            if (member is FieldInfo f)
            {
                var memberType = f.FieldType;
                var convertedValue = DictionaryConverter.ConvertToCompatibleDictionary(value, memberType);

                if (!f.IsStatic)
                {
                    error = "Field not static";
                    return false;
                }


                f.SetValue(null, convertedValue);
            }
            else if (member is PropertyInfo p)
            {
                var memberType = p.PropertyType;
                var convertedValue = DictionaryConverter.ConvertToCompatibleDictionary(value, memberType);

                var setm = p.GetSetMethod(true);
                if (setm is null || !setm.IsStatic)
                {
                    error = "Property setter not static";
                    return false;
                }

                p.SetValue(null, convertedValue);
            }
            return true;
        }
        catch (Exception ex)
        {
            error = ex.InnerException?.Message;
            return false;
        }
    }

    private static Type? ResolveOwnerType(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return null;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var t = asm.GetType(fullName!, false);
                if (t is not null)
                    return t;
            }
            catch { }
        }
        return null;
    }

    internal static bool TryGetGenericDictionaryKV(Type t, out Type keyType, out Type valueType)
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

}
