using System.Collections;
using ReactiveUI;
using Convert = LiveTweak.Infrastructure.Helper.Convert;

namespace LiveTweak.Editor.ViewModels;

public sealed class CollectionState : ReactiveObject
{
    public string Id { get; }
    public string Label { get; }

    public Type type;

    public string? Default
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Error
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ICollection? Value
    {
        get;
        set
        {
            var oldValue = field;
            if (oldValue != value)
            {
                this.RaiseAndSetIfChanged(ref field, value);
                Validate(value);
                field = value;
            }

        }
    }

    public string? Current
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public CollectionState(string id, string label, ICollection? @default, ICollection? value, Type type)
    {
        Id = id;
        Label = label;
        this.type = type;

        var defValue = Helper.Helper.CollectionToString(@default);
        Default = defValue == string.Empty
            ? "Empty collection"
            : defValue;

        var currValue = Helper.Helper.CollectionToString(value);
        Current = currValue == string.Empty
            ? "Empty collection"
            : currValue;

        Value = value;
    }

    private void Validate(object? currentValue)
    {
        try
        {
            Convert.ChangeType(currentValue, type);
            Error = null;
        }
        catch (Exception convEx)
        {
            Error = $"{convEx.Message} (Type: {type.Name})";
        }

    }
}
