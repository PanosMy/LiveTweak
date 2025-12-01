namespace LiveTweak.Infrastructure.Helper;

internal static class Convert
{
    internal static object ChangeType(string? value, Type type)
    {
        object valueWithType;
        Type valueType = type;

        if (valueType.IsEnum)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Cannot convert null to an enum type.");

            valueWithType = Enum.Parse(valueType, value);
        }
        else
        {
            valueWithType = System.Convert.ChangeType(value, valueType);
        }

        return valueWithType;
    }
}
