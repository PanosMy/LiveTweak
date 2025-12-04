namespace LiveTweak.Domain.Models;

public enum TweakCommandType
{
    SetValue,
    SetDictionaryValue,
    SetCollectionValue,
    RevertValue,
    RevertDictionaryValue,
    RevertCollectionValue,
    InvokeAction
}
