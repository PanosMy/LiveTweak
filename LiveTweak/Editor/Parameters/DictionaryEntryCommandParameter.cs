using LiveTweak.Editor.ViewModels;

namespace LiveTweak.Editor.Parameters;

public sealed class DictionaryEntryCommandParameter
{
    public DictionaryState DictionaryState { get; set; }
    public DictionaryEntryViewModel Entry { get; set; }
}
