using System.Collections;
using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;

namespace LiveTweak.Editor.ViewModels;

public sealed class DictionaryState : ReactiveObject
{
    public string Id { get; }
    public string Label { get; }

    private readonly IDictionary defaults;
    private readonly Type keyType;
    private readonly Type valueType;
    private readonly double max;
    private readonly double min;

    public ObservableCollection<DictionaryEntryViewModel> DictionaryEntryViewModels { get; }

    public ReactiveCommand<Unit, Unit> AddEntryCommand { get; }

    public ReactiveCommand<DictionaryEntryViewModel, Unit> RemoveEntryCommand { get; }

    public bool AddButtonEnabled
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool RemoveButtonEnabled
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    public DictionaryState(string id, string label, IDictionary defaults, IDictionary currentValues, double min, double max, Type keyType, Type valueType)
    {
        Id = id;
        Label = label;
        this.defaults = defaults;
        this.keyType = keyType;
        this.valueType = valueType;
        DictionaryEntryViewModels = [];
        AddEntryCommand = ReactiveCommand.Create(AddEmptyEntry);
        RemoveEntryCommand = ReactiveCommand.Create<DictionaryEntryViewModel>(RemoveEntry);
        this.min = min;
        this.max = max;
        SetDictionary(currentValues);
        AddButtonEnabled = double.IsNaN(max) || DictionaryEntryViewModels.Count <= max;
        RemoveButtonEnabled = double.IsNaN(min) || DictionaryEntryViewModels.Count > min;
    }


    internal IDictionary TakeDictionary()
    {
        var dictionary = new Dictionary<object, object?>();
        foreach (var e in DictionaryEntryViewModels)
        {
            var key = e.Key ?? string.Empty;
            dictionary[key] = e.Value;
        }

        return dictionary;
    }


    internal void SetDictionary(IDictionary dictionary)
    {
        DictionaryEntryViewModels.Clear();

        foreach (DictionaryEntry entry in dictionary)
        {
            var key = entry.Key;
            var value = entry.Value;
            var defaultKey = defaults?.Contains(key) == true ? key : null;
            var defaultValue = defaults?[key] ?? value;
            var entryViewModel = new DictionaryEntryViewModel(key, value, defaultValue, defaultKey, keyType, valueType);
            DictionaryEntryViewModels.Add(entryViewModel);
        }

        EnableOrDisableButtons();
    }

    internal void AddEmptyEntry()
    {
        var entryViewModel = new DictionaryEntryViewModel(null, null, null, null, keyType, valueType);
        DictionaryEntryViewModels.Add(entryViewModel);

        EnableOrDisableButtons();
    }

    private void RemoveEntry(DictionaryEntryViewModel model)
    {
        DictionaryEntryViewModels.Remove(model);
        EnableOrDisableButtons();
    }

    private void EnableOrDisableButtons()
    {
        AddButtonEnabled = double.IsNaN(max) || DictionaryEntryViewModels.Count < max;
        RemoveButtonEnabled = double.IsNaN(min) || DictionaryEntryViewModels.Count > min;
    }

}

