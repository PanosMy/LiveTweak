using System.ComponentModel;
using Convert = LiveTweak.Infrastructure.Helper.Convert;

namespace LiveTweak.Editor.ViewModels;

public sealed class DictionaryEntryViewModel : INotifyPropertyChanged
{
    private readonly Type valueType;

    public string Key { get; }

    public string? Value
    {
        get;
        set
        {
            field = value;
            HaveChange = !Equals(Default, Value);
            SetButtonEnabled = !Equals(Current, Value) && IsValidValueForType(value ?? string.Empty);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }

    public string? Current
    {
        get; set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Current)));
        }
    }

    public object? Default { get; set; }

    public string KeyTypeName { get; set; }

    public string ValueTypeName { get; set; }

    public string? Error
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Error)));
        }
    }

    public bool IsBooleanType
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBooleanType)));
        }
    }

    public bool SetButtonEnabled
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SetButtonEnabled)));
        }
    }

    public bool HaveChange
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HaveChange)));
        }
    }

    public DictionaryEntryViewModel(string key, string? value, string? defualt, Type keyType, Type valueType)
    {
        Key = key;
        Default = defualt;
        this.valueType = valueType;
        Value = value;
        Current = value;
        KeyTypeName = keyType.Name;
        ValueTypeName = valueType.Name;
        SetButtonEnabled = false;
        IsBooleanType = valueType == typeof(bool);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool IsValidValueForType(string value)
    {
        var type = valueType;
        try
        {
            if (type == typeof(string))
            {
                Error = null;
                return true; // All strings are valid
            }

            if (type.IsEnum)
            {
                if (Enum.TryParse(type, value, true, out _))
                {
                    Error = null;
                    return true;
                }

                Error = $"'{value}' is not a valid value for enum {type.Name}.";
                return false;
            }

            if (type == typeof(int))
            {
                if (int.TryParse(value, out _))
                {
                    Error = null;
                    return true;
                }

                Error = $"'{value}' is not a valid integer.";
                return false;
            }

            if (type == typeof(float))
            {
                if (float.TryParse(value, out _))
                {
                    Error = null;
                    return true;
                }

                Error = $"'{value}' is not a valid float.";
                return false;
            }

            if (type == typeof(double))
            {
                if (double.TryParse(value, out _))
                {
                    Error = null;
                    return true;
                }

                Error = $"'{value}' is not a valid double.";
                return false;
            }

            if (type == typeof(long))
            {
                if (long.TryParse(value, out _))
                {
                    Error = null;
                    return true;
                }

                Error = $"'{value}' is not a valid long (Int64).";
                return false;
            }

            if (type == typeof(bool))
            {
                if (bool.TryParse(value, out _))
                {
                    Error = null;
                    return true;
                }

                Error = $"'{value}' is not a valid boolean.";
                return false;
            }

            if (type == typeof(DateTime))
            {
                if (DateTime.TryParse(value, out _))
                {
                    Error = null;
                    return true;
                }

                Error = $"'{value}' is not a valid DateTime.";
                return false;
            }

            try
            {
                Convert.ChangeType(value, type);

                Error = null;
                return true;
            }
            catch (Exception convEx)
            {
                Error = $"{convEx.Message} (Type: {type.Name})";
                return false;
            }
        }
        catch (Exception ex)
        {
            Error = $"General validation failure: {ex.Message}";
            return false;
        }
    }
}
