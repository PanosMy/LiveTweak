using System.Collections;
using System.Reflection;
using LiveTweak.Domain.Models;

namespace LiveTweak.Infrastructure.Services;

internal static class CollectionReflection
{
    public static ICollection? TryGetCurrentOrDefault(CollectionEntry collection)
    {
        var current = TryGetCurrent(collection);
        if (current != null)
            return current;

        return collection.Defaults;
    }


    private static ICollection? TryGetCurrent(CollectionEntry collection)
    {
        var ownerType = ResolveOwnerType(collection.OwnerType);
        if (ownerType == null)
        {
            return null;
        }

        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        var member = (MemberInfo?)ownerType.GetField(collection.Name, flags)
                            ?? ownerType.GetProperty(collection.Name, flags);
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

        if (dictObj is ICollection coll)
        {
            return coll;
        }

        return null;
    }

    public static bool TrySetStaticValue(CollectionEntry collection, ICollection? value, out string? error)
    {
        error = null;
        try
        {
            var ownerType = ResolveOwnerType(collection.OwnerType);
            if (ownerType == null)
            {
                error = "Owner type not found";
                return false;
            }

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            var member = (MemberInfo?)ownerType.GetField(collection.Name, flags)
                                 ?? ownerType.GetProperty(collection.Name, flags);

            if (member == null)
            {
                error = "Collection member not found";
                return false;
            }

            if (member is FieldInfo f)
            {
                var memberType = f.FieldType;
                var convertedValue = Helper.Convert.ChangeType(value, memberType);

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
                var convertedValue = Helper.Convert.ChangeType(value, memberType);

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
