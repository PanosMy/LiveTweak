namespace LiveTweak.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class LiveTweakActionAttribute : Attribute
{
    public string Label { get; }
    public string Category { get; }

    public LiveTweakActionAttribute() { }

    public LiveTweakActionAttribute(string label)
    {
        Label = label;
    }

    public LiveTweakActionAttribute(string label, string category)
    {
        Label = label;
        Category = category;
    }
}
