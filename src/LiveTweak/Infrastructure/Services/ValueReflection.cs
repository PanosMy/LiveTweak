using System.Reflection;
using LiveTweak.Domain.Models;


namespace LiveTweak.Infrastructure.Services;

public static class ValueReflection
{
    public static string? TryGetDefault(ValueEntry v)
    {
        var t = v.GetType();
        var prop = t.GetProperty("DefaultValue")
                         ?? t.GetProperty("Default")
                         ?? t.GetProperty("InitialValue")
                         ?? t.GetProperty("Baseline");

        return prop?.GetValue(v)?.ToString();
    }

    public static string? TryGetCurrent(ValueEntry v)
    {
        try
        {
            var ownerType = ResolveOwnerType(v.OwnerType);
            if (ownerType is null)
                return null;

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            var member = (MemberInfo?)ownerType.GetField(v.Name, flags)
                                 ?? ownerType.GetProperty(v.Name, flags);

            if (member is null)
                return null;

            object? val = member switch
            {
                FieldInfo fi when fi.IsStatic => fi.GetValue(null),
                PropertyInfo pi when pi.GetGetMethod(true)?.IsStatic == true => pi.GetValue(null),
                _ => null
            };
            return val?.ToString();
        }
        catch
        {
            return null;
        }
    }

    public static string? TryGetCurrentOrDefault(ValueEntry v) =>
            TryGetCurrent(v) ?? TryGetDefault(v);

    public static bool TrySetStaticValue(ValueEntry valueEntry, string? raw, out string? error)
    {
        error = null;
        try
        {
            if (raw is null)
            {
                error = "Raw value is null";
                return false;
            }

            var ownerType = ResolveOwnerType(valueEntry.OwnerType);
            if (ownerType is null)
            {
                error = "Owner type not found";
                return false;
            }

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            var member = (MemberInfo?)ownerType.GetField(valueEntry.Name, flags)
                                 ?? ownerType.GetProperty(valueEntry.Name, flags);
            if (member is null)
            {
                error = "Member not found";
                return false;
            }

            var valueType = member is FieldInfo fi ? fi.FieldType : ((PropertyInfo)member).PropertyType;
            object parsed =
                    valueType.IsEnum ? Enum.Parse(valueType, raw, true) :
                    valueType == typeof(int) ? int.Parse(raw) :
                    valueType == typeof(float) ? float.Parse(raw) :
                    valueType == typeof(double) ? double.Parse(raw) :
                    valueType == typeof(bool) ? bool.Parse(raw) :
                    valueType == typeof(long) ? long.Parse(raw) :
                    valueType == typeof(string) ? raw :
                    throw new NotSupportedException("Unsupported type " + valueType);

            if (member is FieldInfo f)
            {
                if (!f.IsStatic)
                {
                    error = "Field not static";
                    return false;
                }

                if (!double.IsNaN(valueEntry.Min))
                {
                    var valAsDouble = Convert.ToDouble(parsed);
                    if (valAsDouble < valueEntry.Min)
                    {
                        error = $"Value {valAsDouble} below minimum {valueEntry.Min}";
                        return false;
                    }
                }

                if (!double.IsNaN(valueEntry.Max))
                {
                    var valAsDouble = Convert.ToDouble(parsed);
                    if (valAsDouble > valueEntry.Max)
                    {
                        error = $"Value {valAsDouble} above maximum {valueEntry.Max}";
                        return false;
                    }
                }


                f.SetValue(null, parsed);
            }
            else if (member is PropertyInfo p)
            {
                var setm = p.GetSetMethod(true);
                if (setm is null || !setm.IsStatic)
                {
                    error = "Property setter not static";
                    return false;
                }

                p.SetValue(null, parsed);
            }
            return true;
        }
        catch (Exception ex) { error = ex.Message; return false; }
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
