using System.Reflection;
using LiveTweak.Domain.Models;

namespace LiveTweak.Infrastructure.Reflection;

internal static class DictionaryReflection
{
    public static string? TryGetCurrentOrDefault(DictionaryEntry d, string key)
    {
        var current = TryGetCurrent(d, key);
        if (!string.IsNullOrEmpty(current))
            return current;

        var defaul = TryGetDefault(d, key);
        if (!string.IsNullOrEmpty(defaul))
            return defaul;

        return null;
    }

    public static string? TryGetDefault(DictionaryEntry d, string key)
    {
        if (d.Defaults != null && d.Defaults.TryGetValue(key ?? string.Empty, out var def))
            return def?.ToString();

        return null;
    }

    private static string? TryGetCurrent(DictionaryEntry d, string key)
    {
        var ownerType = ResolveOwnerType(d.OwnerType);
        if (ownerType == null)
        {
            return null;
        }

        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        var member = (MemberInfo?)ownerType.GetField(d.Name, flags)
                            ?? ownerType.GetProperty(d.Name, flags);
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

        if (dictObj is System.Collections.IDictionary dict)
        {
            var keyWithType = Convert.ChangeType(key, d.KeyType);
            if (dict.Contains(keyWithType))
            {
                return dict[keyWithType]?.ToString();
            }
        }

        return null;
    }

    public static bool TrySetStaticValue(DictionaryEntry d, string key, string? value, out string? error)
    {
        error = null;
        try
        {
            var ownerType = ResolveOwnerType(d.OwnerType);
            if (ownerType == null)
            {
                error = "Owner type not found";
                return false;
            }

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            var member = (MemberInfo?)ownerType.GetField(d.Name, flags)
                                 ?? ownerType.GetProperty(d.Name, flags);

            if (member == null)
            {
                error = "Dictionary member not found";
                return false;
            }

            object? dictObj =
                member is FieldInfo fi ? fi.GetValue(null) :
                member is PropertyInfo pi ? pi.GetValue(null) :
                null;


            if (dictObj is not System.Collections.IDictionary dict)
            {
                error = "Target is not a dictionary";
                return false;
            }

            var keyWithType = Convert.ChangeType(key, d.KeyType);
            var valueWithType = Convert.ChangeType(value, d.ValueType);

            dict[keyWithType] = valueWithType;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
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

}
