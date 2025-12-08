namespace LiveTweak.Editor.Helper;

internal static class Helper
{

    public static string? CollectionToString(object? value)
    {
        if (value == null)
            return string.Empty;

        if (value is int[] ints)
            return string.Join(", ", ints);

        if (value is System.Collections.ICollection collection)
        {
            return string.Join(", ", collection.Cast<object>());
        }
        return value.ToString() ?? string.Empty;
    }

}
