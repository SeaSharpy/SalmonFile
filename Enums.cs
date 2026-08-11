namespace Salmon.Levels.Enums;

/// <summary>Identifies the storage category containing a level.</summary>
public enum StoredMode : byte
{
    /// <summary>A level created locally by the player.</summary>
    Created,
    /// <summary>A level downloaded from an external source.</summary>
    Downloaded,
    /// <summary>A level bundled with the game.</summary>
    Main,
}

/// <summary>Controls whether a wall is rendered, collidable, or both.</summary>
public enum WallMode : byte
{
    /// <summary>The wall is rendered and has collision.</summary>
    [InspectorName("visuals & collision")]
    VisualsCollision,
    /// <summary>The wall has collision but is not rendered.</summary>
    [InspectorName("collision")]
    Collision,
    /// <summary>The wall is rendered but has no collision.</summary>
    [InspectorName("visuals")]
    Visuals,
    [InspectorName("trigger")]
    Trigger
}

/// <summary>Identifies one of the four horizontal world-axis directions.</summary>
public enum CardinalDirection : byte
{
    /// <summary>The negative Z-axis direction.</summary>
    [InspectorName("Z-")]
    ZNeg,
    /// <summary>The positive Z-axis direction.</summary>
    [InspectorName("Z+")]
    ZPos,
    /// <summary>The negative X-axis direction.</summary>
    [InspectorName("X-")]
    XNeg,
    /// <summary>The positive X-axis direction.</summary>
    [InspectorName("X+")]
    XPos,
}

/// <summary>Specifies an arithmetic operation applied by a counter trigger.</summary>
public enum Operator : byte
{
    /// <summary>Adds the operand to the counter.</summary>
    [InspectorName("add")]
    Add,
    /// <summary>Subtracts the operand from the counter.</summary>
    [InspectorName("subtract")]
    Subtract,
    /// <summary>Multiplies the counter by the operand.</summary>
    [InspectorName("multiply")]
    Multiply,
    /// <summary>Divides the counter by the operand.</summary>
    [InspectorName("divide")]
    Divide,
    /// <summary>Replaces the counter with the operand.</summary>
    [InspectorName(name: "set")]
    Set
}

internal enum Comparison : byte
{
    [InspectorName("Equal To")]
    Equal,
    [InspectorName("Not Equal To")]
    NotEqual,
    [InspectorName("Greater Than")]
    Greater,
    [InspectorName("Less Than")]
    Less,
    [InspectorName("Greater Than Or Equal To")]
    GreaterOrEqual,
    [InspectorName("Less Than Or Equal To")]
    LessOrEqual
}

/// <summary>Specifies how a toggle trigger changes an object's enabled state.</summary>
public enum ToggleMode : byte
{
    /// <summary>Enables the object.</summary>
    [InspectorName("on")]
    On,
    /// <summary>Disables the object.</summary>
    [InspectorName("off")]
    Off,
    /// <summary>Inverts the object's current enabled state.</summary>
    [InspectorName("flip")]
    Flip
}

/// <summary>Specifies how a movement or rotation trigger interprets its speed value.</summary>
public enum TriggerSpeedMode : byte
{
    /// <summary>The value is a movement or rotation rate per second.</summary>
    [InspectorName("units/second")]
    WorldSpeed,
    /// <summary>The value is the total transition duration in seconds.</summary>
    [InspectorName("seconds in total")]
    TimeSeconds
}

/// <summary>Specifies the mathematical curve used to interpolate a trigger's motion.</summary>
public enum EasingType
{
    /// <summary>Interpolates at a constant rate.</summary>
    [InspectorName("linear")]
    Linear,
    /// <summary>Uses a quadratic polynomial curve.</summary>
    [InspectorName("quadratic")]
    Quadratic,
    /// <summary>Uses a cubic polynomial curve.</summary>
    [InspectorName("cubic")]
    Cubic,
    /// <summary>Uses a sinusoidal curve.</summary>
    [InspectorName("sine")]
    Sine,
    /// <summary>Uses a circular curve.</summary>
    [InspectorName("circular")]
    Circ,
    /// <summary>Uses an exponential curve.</summary>
    [InspectorName("exponential")]
    Expo,
    /// <summary>Uses an overshooting back curve.</summary>
    [InspectorName("back")]
    Back,
    /// <summary>Uses a bouncing curve.</summary>
    [InspectorName("bounce")]
    Bounce,
    /// <summary>Uses an oscillating elastic curve.</summary>
    [InspectorName("elastic")]
    Elastic
}

/// <summary>Specifies where easing is applied during a transition.</summary>
public enum EasingDirection
{
    /// <summary>Applies easing at the start of the transition.</summary>
    [InspectorName("in")]
    In,
    /// <summary>Applies easing at the end of the transition.</summary>
    [InspectorName("out")]
    Out,
    /// <summary>Applies easing at both the start and end of the transition.</summary>
    [InspectorName("in out")]
    InOut
}

/// <summary>Identifies the transform that objects follow.</summary>
public enum FollowTarget : byte
{
    /// <summary>Objects follow the game camera.</summary>
    [InspectorName("camera")]
    Camera,
    /// <summary>Objects follow the player.</summary>
    [InspectorName("player")]
    Player,
}
