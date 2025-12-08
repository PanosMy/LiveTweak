using System.Collections;
using LiveTweak.Infrastructure.Services;

namespace LiveTweak.Infrastructure.Helper;

internal static class DictionaryConverter
{
    public static object ConvertToCompatibleDictionary(IDictionary? source, Type targetDictionaryType)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(targetDictionaryType);

        if (targetDictionaryType.IsAssignableFrom(source.GetType()))
            return source;

        if (typeof(IDictionary).IsAssignableFrom(targetDictionaryType) && !DictionaryReflection.TryGetGenericDictionaryKV(targetDictionaryType, out _, out _))
        {
            var ht = new Hashtable();
            foreach (DictionaryEntry de in source)
                ht[de.Key] = de.Value;

            if (targetDictionaryType.IsAssignableFrom(ht.GetType()))
                return ht;

            if (!targetDictionaryType.IsInterface && !targetDictionaryType.IsAbstract)
            {
                var inst = Activator.CreateInstance(targetDictionaryType);
                if (inst is IDictionary dictInst)
                {
                    foreach (DictionaryEntry de in ht)
                        dictInst[de.Key] = de.Value;
                    return dictInst;
                }
            }

            return ht;
        }


        if (DictionaryReflection.TryGetGenericDictionaryKV(targetDictionaryType, out var keyType, out var valueType))
        {
            var concrete = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            var newInst = Activator.CreateInstance(concrete) ?? throw new InvalidOperationException("Failed to create concrete generic dictionary instance.");

            var addMethod = concrete.GetMethod("Add", [keyType, valueType])
                            ?? throw new InvalidOperationException("Add method not found on concrete dictionary.");

            foreach (DictionaryEntry de in source)
            {
                var keyConv = Convert.ChangeType(de.Key, keyType);
                var valConv = Convert.ChangeType(de.Value, valueType);
                addMethod.Invoke(newInst, [keyConv, valConv]);
            }

            return newInst;
        }

        throw new InvalidOperationException($"Target type {targetDictionaryType.FullName} is not a recognized dictionary type.");
    }
}
