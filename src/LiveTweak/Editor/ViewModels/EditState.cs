using LiveTweak.Application.Helpers;
using LiveTweak.Domain.Models;
using ReactiveUI;

namespace LiveTweak.Editor.ViewModels;

public sealed class EditState : ReactiveObject
{
    public string Id { get; }
    public string Label { get; }
    public SchemaValueKind ValueKind { get; }

    public double? Min { get; }
    public double? Max { get; }

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

    public string? Edited
    {
        get;
        set
        {
            var oldValue = field;
            if (oldValue != value)
            {
                this.RaiseAndSetIfChanged(ref field, value);
                Validate(value);
                HaveChange = Edited != Default;
                field = value;
            }

        }
    }

    public bool IsBooleanType
        => ValueKind == SchemaValueKind.Boolean;

    public bool SetButtonEnabled
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool HaveChange
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? MinText
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? MaxText
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Current
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public EditState(string id, string label, string? @default, string? edited,
                     SchemaValueKind kind, double min, double max)
    {
        Id = id;
        Default = @default;
        Label = label;
        Edited = edited;
        ValueKind = kind;

        Min = !double.IsNaN(min)
            ? min
            : null;
        Max = !double.IsNaN(max)
            ? max
            : null;

        MinText = Min?.ToString();
        MaxText = Max?.ToString();
        SetButtonEnabled = false;
        Current = edited;
    }

    private void Validate(string? currentValue)
    {
        if (currentValue == null)
        {
            return;
        }

        switch (ValueKind)
        {
            case SchemaValueKind.Integer:
                {
                    var (isValid, error) = MathHelper.IsWithinRange<int>(currentValue, Min, Max);
                    Error = error;
                    SetButtonEnabled = isValid;

                    break;
                }

            case SchemaValueKind.Float:
                {
                    var (isValid, error) = MathHelper.IsWithinRange<float>(currentValue, Min, Max);
                    Error = error;
                    SetButtonEnabled = isValid;
                    break;
                }

            case SchemaValueKind.Double:
                {
                    var (isValid, error) = MathHelper.IsWithinRange<double>(currentValue, Min, Max);
                    Error = error;
                    SetButtonEnabled = isValid;
                    break;
                }
            case SchemaValueKind.Boolean:
                {
                    IsBoolean(currentValue);
                    break;
                }
            default:
                SetButtonEnabled = true;
                break;

        }
    }

    private void IsBoolean(string value)
    {
        if (bool.TryParse(value, out var parsedValue))
        {
            SetButtonEnabled = true;
            Error = default;
            return;
        }

        Error = "Invalid boolean format";
        SetButtonEnabled = false;
    }
}
