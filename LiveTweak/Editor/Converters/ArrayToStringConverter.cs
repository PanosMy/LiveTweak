using System.Globalization;
using System.Text.Json;
using Avalonia.Data.Converters;

namespace LiveTweak.Editor.Converters;

public class ArrayToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Helper.Helper.CollectionToString(value);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = (value as string)?.Trim();
        if (string.IsNullOrEmpty(s))
        {
            if (targetType.IsArray)
            {
                var elem = targetType.GetElementType() ?? typeof(int);
                return Array.CreateInstance(elem, 0);
            }
            return Array.Empty<int>();
        }

        if (s.StartsWith('[') && s.EndsWith(']'))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<int[]>(s);
                return parsed ?? [];
            }
            catch
            {
            }
        }

        var parts = s.Split([','], StringSplitOptions.RemoveEmptyEntries)
                     .Select(p => p.Trim())
                     .Where(p => !string.IsNullOrEmpty(p));

        return parts.ToArray();
    }
}
