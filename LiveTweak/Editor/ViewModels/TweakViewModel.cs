using System.Collections;
using System.Collections.ObjectModel;
using System.Reactive;
using LiveTweak.Application.Abstractions;
using LiveTweak.Domain.Abstractions;
using LiveTweak.Domain.Models;
using LiveTweak.Editor.Parameters;
using LiveTweak.Infrastructure.Reflection;
using LiveTweak.Infrastructure.Services;
using ReactiveUI;
using DictEntry = LiveTweak.Domain.Models.DictionaryEntry;

namespace LiveTweak.Editor.ViewModels;

public sealed class TweakViewModel : ReactiveObject
{
    private readonly ITweakSchemaProvider _schemaProvider;
    private readonly ITweakCommandDispatcher _dispatcher;

    public ObservableCollection<string> Categories { get; } = [];
    public ObservableCollection<object> CurrentItems { get; } = [];

    private TweakEntry[] _all = [];
    private readonly ObservableCollection<EditState> _edits = [];
    private readonly ObservableCollection<DictionaryState> _dictEdits = [];
    private readonly Dictionary<string, SchemaEntry> _rawIndex = [];
    public string? SelectedCategory
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            if (_selectedCategory != default && _edits.Count > 0)
            {
                BuildCurrentItems();
                if (value != null)
                    _selectedCategory = value;
            }

        }
    }

    private string? _selectedCategory;
    public string Status
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "Idle";

    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    public ReactiveCommand<EditState, Unit> SetValueCommand { get; }
    public ReactiveCommand<EditState, Unit> RevertValueCommand { get; }
    public ReactiveCommand<DictionaryState, Unit> SetDictionaryValueCommand { get; }
    public ReactiveCommand<DictionaryEntryCommandParameter, Unit> RevertDictionaryValueCommand { get; }
    public ReactiveCommand<TweakEntry, Unit> InvokeActionCommand { get; }

    public TweakViewModel()
    {
        ITweakSource source = new ReflectionTweakSource(new AttributeReaderAdapter());
        _schemaProvider = new ReflectionSchemaProvider(source);
        _dispatcher = new ReflectionCommandDispatcher(source);

        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync);
        SetValueCommand = ReactiveCommand.CreateFromTask<EditState>(SetValueAsync);
        RevertValueCommand = ReactiveCommand.CreateFromTask<EditState>(RevertValueAsync);
        SetDictionaryValueCommand = ReactiveCommand.CreateFromTask<DictionaryState>(SetDictionaryValueAsync);
        RevertDictionaryValueCommand = ReactiveCommand.CreateFromTask<DictionaryEntryCommandParameter>(RevertDictionaryValueAsync);
        InvokeActionCommand = ReactiveCommand.CreateFromTask<TweakEntry>(InvokeAsync);

        var raw = source.BuildSchema()?.ToArray() ?? [];
        foreach (var e in raw)
        {
            var id = $"{e.OwnerType ?? "OwnerUnknown"}:{e.Name}";
            _rawIndex[id] = e;
        }
    }

    private async Task RefreshAsync()
    {
        Status = "Loading...";
        _all = [.. await _schemaProvider.GetSchemaAsync()];

        Categories.Clear();
        foreach (var c in _all.Select(e => e.Category).Distinct().OrderBy(x => x))
            Categories.Add(c);

        SelectedCategory = _selectedCategory != null
            ? _selectedCategory
            : _selectedCategory = Categories.FirstOrDefault();

        _edits.Clear();
        foreach (var v in _all.Where(e => e.Kind == TweakEntryKind.Value))
        {

            if (_rawIndex.TryGetValue(v.Id, out var rawEntry) && rawEntry is ValueEntry valueEntry)
            {
                var current = ValueReflection.TryGetCurrentOrDefault(valueEntry);
                var def = valueEntry.DefaultValue?.ToString();
                var min = valueEntry.Min;
                var max = valueEntry.Max;

                _edits.Add(new EditState(v.Id, v.Label, def, current, v.ValueKind, min, max));
            }
        }

        _dictEdits.Clear();
        foreach (var v in _all.Where(e => e.Kind == TweakEntryKind.Dictionary))
        {

            if (_rawIndex.TryGetValue(v.Id, out var rawEntry) && rawEntry is DictEntry dictionaryEntry)
            {
                var defualt = dictionaryEntry.Defaults;
                var currunt = DictionaryReflection.TryGetCurrentOrDefault(dictionaryEntry);
                _dictEdits.Add(new DictionaryState(v.Id, v.Label, defualt, currunt, dictionaryEntry.Min, dictionaryEntry.Max, dictionaryEntry.KeyType, dictionaryEntry.ValueType));
            }
        }

        BuildCurrentItems();
        Status = $"Loaded {Categories.Count} categories";
    }

    private void BuildCurrentItems()
    {
        CurrentItems.Clear();
        if (SelectedCategory is null)
            return;

        CurrentItems.Add(new HeaderMarker(SelectedCategory));

        foreach (var e in _all.Where(x => x.Category == SelectedCategory))
        {
            if (e.Kind == TweakEntryKind.Value)
            {
                var st = _edits.First(s => s.Id == e.Id);
                CurrentItems.Add(st);
            }
            else if (e.Kind == TweakEntryKind.Dictionary)
            {
                var st = _dictEdits.First(s => s.Id == e.Id);
                CurrentItems.Add(st);
            }
            else
            {
                CurrentItems.Add(e);
            }
        }
    }

    private async Task SetValueAsync(EditState state)
    {
        var cmd = new TweakCommand<string>(TweakCommandType.SetValue, state.Id, state.Edited);
        var res = await _dispatcher.DispatchAsync(cmd);

        Status = res.Ok ? $"Set '{state.Label}' = {res.NewValue}" : $"Set failed '{state.Label}': {res.Message}";
        if (res.Ok)
        {
            state.Edited = res.NewValue;
            state.SetButtonEnabled = false;
            state.Current = res.NewValue;
        }
    }

    private async Task RevertValueAsync(EditState state)
    {
        var cmd = new TweakCommand<string>(TweakCommandType.RevertValue, state.Id);
        var res = await _dispatcher.DispatchAsync(cmd);

        Status = res.Ok ? $"Reverted '{state.Label}'" : $"Revert failed '{state.Label}': {res.Message}";
        if (res.Ok)
        {
            state.Edited = res.NewValue;
            state.SetButtonEnabled = false;
            state.Current = res.NewValue;
        }
    }

    private async Task SetDictionaryValueAsync(DictionaryState state)
    {
        var id = state.Id;
        var dictionary = state.TakeDictionary();
        var cmd = new TweakCommand<IDictionary>(TweakCommandType.SetDictionaryValue, id, dictionary);
        var res = await _dispatcher.DispatchAsync(cmd);

        Status = res.Ok ? $"Set '{state.Label}' Successful" : $"Set failed '{state.Label}': {res.Message}";
        if (res.Ok)
        {
            state.SetDictionary(res.NewValue!);
        }
    }

    private async Task RevertDictionaryValueAsync(DictionaryEntryCommandParameter state)
    {
        var id = state.DictionaryState.Id;
        var label = state.DictionaryState.Label;
        var key = state.Entry?.Key;
        var cmd = new TweakCommand<IDictionary>(TweakCommandType.RevertDictionaryValue, id, Key: key);
        var res = await _dispatcher.DispatchAsync(cmd);

        Status = res.Ok ? $"Reverted '{label}'" : $"Revert failed '{label}': {res.Message}";
        if (res.Ok)
        {
            state.DictionaryState.SetDictionary(res.NewValue!);
        }
    }

    private async Task InvokeAsync(TweakEntry entry)
    {
        var cmd = new TweakCommand<string>(TweakCommandType.InvokeAction, entry.Id);
        var res = await _dispatcher.DispatchAsync(cmd);

        Status = res.Ok ? $"Invoked '{entry.Label}'" : $"Invoke failed '{entry.Label}': {res.Message}";
    }
}

public sealed record HeaderMarker(string Title);

