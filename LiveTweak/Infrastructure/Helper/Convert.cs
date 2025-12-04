using System.Collections;
using System.Globalization;

namespace LiveTweak.Infrastructure.Helper;

internal static class Convert
{
    internal static object ChangeType(object? value, Type type, CultureInfo? culture = null)
    {
        object valueWithType;
        Type valueType = type;
        culture ??= CultureInfo.InvariantCulture;
        if (valueType.IsEnum)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Cannot convert null to an enum type.");

            if (!Enum.TryParse(valueType, value?.ToString() ?? string.Empty, true, out var result) || !Enum.IsDefined(valueType, result))
            {
                throw new ArgumentException($"Value '{value}' is not valid for enum type '{valueType.Name}'.", nameof(value));
            }
            valueWithType = result;
        }
        else if (valueType.GetInterface(nameof(ICollection)) != null)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Cannot convert null to an array type.");

            if (value is not IEnumerable stringArray)
                throw new Exception("Value must be a string array to convert to a collection type.");

            valueWithType = ConvertStringsToCollection(type, stringArray, culture);
        }
        else
        {
            valueWithType = System.Convert.ChangeType(value, valueType, culture);
        }

        return valueWithType;
    }

    public static object ConvertStringsToCollection(Type type, IEnumerable values, CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (values != null)
        {
            culture ??= CultureInfo.InvariantCulture;
            Type elementType;

            if (type.IsArray)
            {
                var valueObject = values.Cast<object?>().ToList();
                elementType = type.GetElementType()!;
                var arrayList = Array.CreateInstance(elementType, valueObject.Count);
                for (var i = 0; i < valueObject.Count; i++)
                {
                    var valueWithType = ChangeType(valueObject[i], elementType, culture);
                    arrayList.SetValue(valueWithType, i);
                }

                return arrayList;
            }

            elementType = type.GetGenericArguments()[0];
            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = (IList)Activator.CreateInstance(listType)!;

            foreach (var value in values)
            {
                var valueWithType = ChangeType(value, elementType, culture);
                list.Add(valueWithType);
            }

            return list;
        }

        throw new ArgumentNullException(nameof(values));
    }


}




