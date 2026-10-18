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
/// <param name="label">The field label when displayed in inspectors. </param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InspectorFieldAttribute(string label = "") : Attribute
{
    /// <summary>The field label when displayed in inspectors. Do not include this on serialization only fields.</summary>
    public string Label => label;

    /// <summary>The visual order when displayed in inspectors. Do not use this on serialization only fields.</summary>
    public uint Order;
    /// <exclude />
    public float OldOrder = float.NaN;
    /// <summary>The serialization field ID. Must stay constant for a field once added.</summary>
    public byte ID;
    /// <summary>The minimum for a number (leave at NaN for none).</summary>
    public float Min = float.NaN;
    /// <summary>The maximum for a number (leave at NaN for none).</summary>
    public float Max = float.NaN;
    /// <summary>The scene handle used to edit the field.</summary>
    public InspectorHandleType Handle;
    /// <summary>Whether the inspector uses a multiline text editor.</summary>
    public bool Multiline;
    /// <summary>The maximum length for a string (leave at -1 for unlimited).</summary>
    public int MaxLength = -1;
    /// <summary>Whether a number field uses a slider in the inspector.</summary>
    public bool Slider;
    /// <exclude />
    public bool NoSave;
    /// <exclude />
    public bool NoInspect;
    /// <exclude />
    public bool Old;
    /// <summary>
    /// A named options source used in the inspector for a string.
    /// Sources:
    /// "Materials"
    /// "Shapes"
    /// "BackgroundMeshes"
    /// "Prefabs" (for Builtin)
    /// "Skins"
    /// "Hats"
    /// </summary>
    public string Options = "";
}

/// <summary>Overrides the name of an enum value when displayed in inspectors.</summary>
/// <param name="name">The name shown in the inspector.</param>
[AttributeUsage(AttributeTargets.Field)]
public sealed class InspectorNameAttribute(string name) : Attribute
{
    /// <summary>The name shown in the inspector.</summary>
    public string Name => name;
}

/// <summary>Exposes a method as a button in the dynamic inspector.</summary>
/// <param name="label">The button label.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class InspectorButtonAttribute(string label) : Attribute
{
    /// <summary>The button label.</summary>
    public string Label => label;
    /// <summary>The visual order (always after fields).</summary>
    public float Order;
}
