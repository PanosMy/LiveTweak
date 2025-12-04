namespace LiveTweak.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
public sealed class LiveTweakAttribute : Attribute
{
    public string Label { get; }
    public double Min { get; set; }
    public double Max { get; set; }
    public string Category { get; }


    // Optional: static method name to call on successful set
    // The method must be static on the constants type. Supported signatures:
    //   void Method()
    //   void Method(TValue value) 
    //   void Method(string member, TValue value)
    public string OnChanged { get; set; }

    public LiveTweakAttribute(string label)
        : this(label, "General")
    {
    }

    public LiveTweakAttribute(string label, string category)
    {
        Label = label;
        Category = category;
        Min = double.NaN;
        Max = double.NaN;
    }
}
