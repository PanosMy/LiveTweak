using System.Reflection;

namespace LiveTweak.Infrastructure.Reflection;


internal static class ReflectionUtils
{
    /// <summary>
    /// Resolves a Type from its full name by searching all loaded assemblies.
    /// </summary>
    internal static Type? ResolveType(string fullName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var t = asm.GetType(fullName, false);
                if (t is not null)
                    return t;
            }
            catch { }
        }
        return null;
    }

    /// <summary>
    /// Resolves an instance of the owner type for non-static method invocation.
    /// Looks for static Instance or Current properties, or creates a new instance.
    /// </summary>
    internal static object? ResolveOwnerInstance(Type t)
    {
        var p = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        if (p != null)
            return p.GetValue(null);
        p = t.GetProperty("Current", BindingFlags.Public | BindingFlags.Static);
        if (p != null)
            return p.GetValue(null);
        var ctor = t.GetConstructor(Type.EmptyTypes);
        return ctor != null ? Activator.CreateInstance(t) : null;
    }
}
