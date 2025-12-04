using System.Collections;
using System.ComponentModel;
using Convert = LiveTweak.Infrastructure.Helper.Convert;

namespace LiveTweak.Editor.ViewModels;

public sealed class DictionaryEntryViewModel : INotifyPropertyChanged
{
    private readonly Type valueType;
    private readonly Type keyType;
    private readonly bool isNew;

    public object? Key
    {
        get;
        set
        {
            if (field?.ToString() == value?.ToString())
            {
                return;
            }


            KeyError = IsValidValueForType(value?.ToString() ?? string.Empty, keyType);
            field = value;
        }
    }

    public object? KeyEnum
    {
        get => Convert.ChangeType(Key, keyType);
        set
        {
            if (!keyType.IsEnum)
            {
                return;
            }

            Key = value;
        }
    }

    public object? KeyCurrent
    {
        get; set
        {
            field = value;
            HaveChange = !isNew && !Equals(ValueDefault, ValueCurrent?.ToString()) && !Equals(KeyDefault, KeyCurrent?.ToString());
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValueCurrent)));
        }
    }

    public string? KeyDefault { get; set; }

    public string KeyTypeName { get; set; }

    public string? KeyError
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KeyError)));
        }
    }

    public object? Value
    {
        get;
        set
        {
            if (ValueIsArray)
            {
                if (field == value)
                {
                    return;
                }
                ValueError = IsValidCollection(value, valueType);
                field = value;
            }


            if (field?.ToString() == value?.ToString())
            {
                return;
            }

            ValueError = IsValidValueForType(value?.ToString() ?? string.Empty, valueType);
            field = value;
        }
    }

    public IEnumerable? ValueArray
    {
        get;
        set
        {
            if (!ValueIsArray)
            {
                return;
            }

            Value = value;
            field = value;
        }
    }

    public object? ValueEnum
    {
        get => Convert.ChangeType(Value, valueType);
        set
        {
            if (!valueType.IsEnum)
            {
                return;
            }

            Value = value;

        }
    }

    public bool ValueBoolean
    {
        get => System.Convert.ToBoolean(Value);
        set
        {
            if (valueType != typeof(bool))
            {
                return;
            }
            Value = value;
        }
    }

    public string? ValueCurrent
    {
        get; set
        {
            field = value;
            HaveChange = !isNew && !Equals(ValueDefault, ValueCurrent?.ToString()) && !Equals(KeyDefault, KeyCurrent?.ToString());
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValueCurrent)));
        }
    }

    public string? ValueDefault { get; set; }

    public string ValueTypeName { get; set; }

    public string? ValueError
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValueError)));
        }
    }

    public bool IsValueTextBoxShowing => !ValueIsEnum && !IsBooleanType && !ValueIsArray;

    public bool IsBooleanType
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBooleanType)));
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

    public bool KeyIsEnum { get; set; }
    public bool ValueIsEnum { get; set; }

    public object? KeyEnums
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HaveChange)));
        }
    }

    public object? ValueEnums
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HaveChange)));
        }
    }

    public bool ValueIsArray
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValueIsArray)));
        }
    }

    public DictionaryEntryViewModel(object? key, object? value, object? defualtValue, object? defaultKey, Type keyType, Type valueType)
    {
        this.valueType = valueType;
        this.keyType = keyType;
        KeyEnums = keyType.IsEnum ? Enum.GetValues(keyType) : null;
        ValueEnums = valueType.IsEnum ? Enum.GetValues(valueType) : null;
        KeyIsEnum = keyType.IsEnum;
        ValueIsEnum = valueType.IsEnum;


        Value = value;
        Key = key;

        isNew = defaultKey == null || defualtValue == null;

        KeyDefault = isNew
            ? "New key"
            : $"Default key: {defaultKey}";
        KeyCurrent = key;
        ValueIsArray = valueType.GetInterface(nameof(ICollection)) != null;
        KeyTypeName = keyType.Name;
        ValueTypeName = valueType.Name;
        IsBooleanType = valueType == typeof(bool);
        HaveChange = !isNew && !Equals(ValueDefault, ValueCurrent?.ToString()) && !Equals(KeyDefault, KeyCurrent?.ToString());

        ValueDefault = isNew
            ? "New Value"
            : ValueIsArray
                ? $"Default Value: {Helper.Helper.CollectionToString(defualtValue)}"
                : $"Default Value: {defualtValue}";

        ValueCurrent = ValueIsArray
            ? $"Current Value: {Helper.Helper.CollectionToString(defualtValue)}"
            : $"Current Value: {defualtValue}";

        if (ValueIsArray)
        {
            ValueArray = value is not IEnumerable enu
                    ? ValueArray = Array.Empty<string>()
                    : ValueArray = enu;
        }

    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private string? IsValidValueForType(string value, Type type)
    {

        try
        {
            if (type == typeof(string))
            {
                return null; // All strings are valid
            }

            if (type.IsEnum)
            {
                if (Enum.TryParse(type, value, true, out _))
                {
                    return null;
                }

                var error = $"'{value}' is not a valid value for enum {type.Name}.";
                return error;
            }

            if (type == typeof(int))
            {
                if (int.TryParse(value, out _))
                {
                    return null;
                }

                var error = $"'{value}' is not a valid integer.";
                return error;
            }

            if (type == typeof(float))
            {
                if (float.TryParse(value, out _))
                {
                    return null;
                }

                var error = $"'{value}' is not a valid float.";
                return error;
            }

            if (type == typeof(double))
            {
                if (double.TryParse(value, out _))
                {
                    return null;
                }

                var error = $"'{value}' is not a valid double.";
                return error;
            }

            if (type == typeof(long))
            {
                if (long.TryParse(value, out _))
                {
                    return null;
                }

                var error = $"'{value}' is not a valid long (Int64).";
                return error;
            }

            if (type == typeof(bool))
            {
                if (bool.TryParse(value, out _))
                {
                    return null;
                }

                var error = $"'{value}' is not a valid boolean.";
                return error;
            }

            if (type == typeof(DateTime))
            {
                if (DateTime.TryParse(value, out _))
                {
                    return null;
                }

                var error = $"'{value}' is not a valid DateTime.";
                return error;
            }

            try
            {
                Convert.ChangeType(value, type);
                return null;
            }
            catch (Exception convEx)
            {
                var error = $"{convEx.Message} (Type: {type.Name})";
                return error;
            }
        }
        catch (Exception ex)
        {
            var error = $"General validation failure: {ex.Message}";
            return error;
        }
    }

    private string? IsValidCollection(object? value, Type type)
    {

        try
        {
            Convert.ChangeType(value, type);
            return null;
        }
        catch (Exception convEx)
        {
            return $"{convEx.Message} (Type: {type.Name})";
        }
    }
}
