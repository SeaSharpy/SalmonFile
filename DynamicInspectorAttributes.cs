namespace Salmon;

[Flags]
public enum InspectorHandleType : byte
{
    None = 0,
    Move = 1,
    Rotate = 2,
    Scale = 4,
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InspectorFieldAttribute : Attribute
{
    public string Label { get; }
    public float Order { get; set; }
    public float Min { get; set; } = float.NaN;
    public float Max { get; set; } = float.NaN;
    public InspectorHandleType Handle { get; set; }
    public bool Multiline { get; set; }
    public bool ReadOnly { get; set; }
    public bool Slider { get; set; }
    public bool NoSave { get; set; }
    public bool NoInspect { get; set; }
    public bool Old { get; set; }
    public string Options { get; set; } = "";
    public InspectorFieldAttribute(string label)
    {
        Label = label;
    }
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class InspectorNameAttribute : Attribute
{
    public string Name { get; }
    public InspectorNameAttribute(string name)
    {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class InspectorButtonAttribute : Attribute
{
    public string Label { get; }
    public float Order { get; set; }
    public InspectorButtonAttribute(string label)
    {
        Label = label;
    }
}
