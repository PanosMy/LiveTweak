using System.Globalization;
using Avalonia.Data.Converters;
using LiveTweak.Editor.Parameters;
using LiveTweak.Editor.ViewModels;

namespace LiveTweak.Editor.Converters;

public sealed class DictionaryEntryCommandParameterConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {

        return new DictionaryEntryCommandParameter
        {
            DictionaryState = values[0] as DictionaryState ?? default!,
            Entry = values[1] as DictionaryEntry ?? default!
        };
    }
}
