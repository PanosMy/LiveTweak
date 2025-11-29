using ReactiveUI;

namespace LiveTweak.Editor.ViewModels;

public sealed class DictionaryState : ReactiveObject
{
    public string Id { get; }
    public string Label { get; }

    public List<DictionaryEntryViewModel> DictionaryEntryViewModels
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public DictionaryState(string id, string label, IReadOnlyDictionary<string, string?> keyValues, List<string?> currentValues, Type keyType, Type valueType)
    {
        Id = id;
        Label = label;
        DictionaryEntryViewModels = [.. keyValues.Select((e, index) => new DictionaryEntryViewModel(e.Key, currentValues[index], e.Value, keyType, valueType))];
    }
}

