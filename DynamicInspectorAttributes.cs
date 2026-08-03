namespace Salmon;

/// <summary>Specifies which scene handle edits an inspector field.</summary>
[Flags]
public enum InspectorHandleType : byte
{
    /// <summary>The field has no scene handle.</summary>
    None = 0,
    /// <summary>The field is edited with a translation handle.</summary>
    Move = 1,
    /// <summary>The field is edited with a rotation handle.</summary>
    Rotate = 2,
    /// <summary>The field is edited with a scale handle.</summary>
    Scale = 4,
}

/// <summary>Describes how a field or property is serialized and displayed in the dynamic inspector.</summary>
/// <param name="label">The label shown in the inspector.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InspectorFieldAttribute(string label) : Attribute
{
    /// <summary>The user-facing field label.</summary>
    public string Label => label;
    /// <summary>The field's sort and serialization key.</summary>
    public float Order;
    /// <summary>The optional minimum numeric value.</summary>
    public float Min = float.NaN;
    /// <summary>The optional maximum numeric value.</summary>
    public float Max = float.NaN;
    /// <summary>The scene handle used to edit the field.</summary>
    public InspectorHandleType Handle;
    /// <summary>Whether the inspector uses a multiline text editor.</summary>
    public bool Multiline;
    /// <summary>Whether numeric values are displayed with a slider.</summary>
    public bool Slider;
    /// <exclude />
    public bool NoSave;
    /// <exclude />
    public bool NoInspect;
    /// <exclude />
    public bool Old;
    /// <summary>The named option source used by the inspector.</summary>
    public string Options = "";
}

/// <summary>Overrides the user-facing name of an inspector enum.</summary>
/// <param name="name">The name shown in the inspector.</param>
[AttributeUsage(AttributeTargets.Field)]
public sealed class InspectorNameAttribute(string name) : Attribute
{
    /// <summary>The name shown in the inspector.</summary>
    public string Name => name;
}

/// <summary>Exposes a method as a button in the dynamic inspector.</summary>
/// <param name="label">The label shown on the button.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class InspectorButtonAttribute(string label) : Attribute
{
    /// <summary>The button label.</summary>
    public string Label => label;
    /// <summary>The button's sort order.</summary>
    public float Order;
}
