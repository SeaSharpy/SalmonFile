namespace Salmon.Levels.Objects;

#if !DEMO_APP
/// <summary>Provides identity data used by every level object.</summary>
public abstract class ObjectDefinition
{
    /// <summary>The object name, unique within its parent group.</summary>
    [InspectorField("Name", Order = -999)]
    public string Name = "Unknown";

    /// <summary>The persistent object identifier.</summary>
    [InspectorField(label: "ID", Order = -1, NoInspect = true)]
    public ulong ID { get; internal set; } = Snowflake.CreateULong();
}


/// <summary>Adds local position, rotation and visuals to a level object.</summary>
public abstract partial class PhysicalObjectDefinition : ObjectDefinition
{
    /// <summary>The local position.</summary>
    [InspectorField("Position", Order = 0, Handle = InspectorHandleType.Move)]
    public Vector3 Position = Vector3.zero;

    /// <summary>The local Euler rotation in degrees.</summary>
    [InspectorField("Rotation", Order = 1, Handle = InspectorHandleType.Rotate)]
    public Vector3 Rotation = Vector3.zero;
}
#endif

/// <summary>Stores a set of persistent level object identifiers.</summary>
public sealed partial class ObjectReferences : ISpecialSerializable
{
    /// <summary>
    /// The referenced object identifiers. 
    /// More than 8 will cause strange UI glitches and more than 255 will not save correctly. 
    /// </summary>
    public readonly HashSet<ulong> IDs = new(8);
    #if !DEMO_APP
    internal static string GetDisplayPath(ulong id, Group root)
    {
        if (root.ID == id)
            return "/";
        var parts = new List<string>();
        if (!TryGetPath(root, id, parts))
            return "???";
        parts.Reverse();
        return "/" + string.Join("/", parts);
    }
    private static bool TryGetPath(Group group, ulong id, List<string> parts)
    {
        foreach (var obj in group.Objects)
        {
            if (obj == null)
                continue;
            if (obj.ID == id)
            {
                parts.Add(obj.Name);
                return true;
            }
            if (obj is Group childGroup && TryGetPath(childGroup, id, parts))
            {
                parts.Add(obj.Name);
                return true;
            }
        }
        return false;
    }
    #endif
    /// <inheritdoc/>
    public void SpecialWrite(BinaryWriter writer)
    {
        writer.Write((byte)IDs.Count);
        foreach (var id in IDs)
            writer.Write(id);
    }
    /// <inheritdoc/>
    public void SpecialRead(BinaryReader reader)
    {
        byte count = reader.ReadByte();
        IDs.Clear();
        for (var i = 0; i < count; i++)
            IDs.Add(reader.ReadUInt64());
    }
}

#if !DEMO_APP
/// <summary>Adds non-uniform local scale to a level object.</summary>
public abstract partial class ScaledObjectDefinition : PhysicalObjectDefinition
{
    /// <summary>The local scale on each axis.</summary>
    [InspectorField("Scale", Order = 2, Min = 0.05f, Handle = InspectorHandleType.Scale)]
    public Vector3 Scale = Vector3.one;
}

/// <summary>Adds uniform local scale to a level object.</summary>
public abstract partial class UniformScaledObjectDefinition : PhysicalObjectDefinition
{
    /// <summary>The uniform local scale.</summary>
    [InspectorField("Scale", Order = 2, Min = 0.05f, Handle = InspectorHandleType.Scale)]
    public float Scale = 1f;
}


/// <summary>Contains an ordered collection of child level objects.</summary>
[ObjectType("Group")]
public sealed partial class Group : UniformScaledObjectDefinition, ISpecialSerializable
{
    /// <summary>Enumerates every descendant in depth-first order.</summary>
    /// <returns>The descendant objects, excluding this group.</returns>
    public IEnumerable<ObjectDefinition> Recurse()
    {
        foreach (var obj in Objects)
        {
            yield return obj;
            if (obj is Group group)
                foreach (var child in group.Recurse())
                    yield return child;
        }
    }
    /// <summary>The group's direct children.</summary>
    public readonly List<ObjectDefinition> Objects = [];
    /// <inheritdoc/>
    public void SpecialWrite(BinaryWriter writer) => SerializeObjects(writer, Objects);
    /// <inheritdoc/>
    public void SpecialRead(BinaryReader reader)
    {
        var count = reader.ReadInt32();
        Objects.Clear();
        if (Objects.Capacity < count)
            Objects.Capacity = count;
        for (var i = 0; i < count; i++)
        {
            var name = reader.ReadString();
            if (name == "Breakable Wall")
                name = "Wall";
            var type = ObjectMetadata.FromDisplayName(name);
            if (type == null)
                continue;
            if (!GeneratedDynamicSerializers.TryRead(type, reader, out var obj))
                obj = DynamicSerializer.Deserialize(type, reader);
            Objects.Add((ObjectDefinition)obj);
        }
    }

    internal static void SerializeObjects(BinaryWriter writer, List<ObjectDefinition> objects)
    {
        writer.Write(objects.Count);
        foreach (var obj in objects)
        {
            writer.Write(obj.GetDisplayBytes());
            if (!GeneratedDynamicSerializers.TryWrite(obj, writer))
                DynamicSerializer.Serialize(obj, writer);
        }
    }
}

[ObjectType("Builtin")]
public partial class Builtin : ScaledObjectDefinition
{
    [InspectorField("Target", Order = 3, Options = "Prefabs")]
    public string Target;
}

/// <summary>Defines a rendered and/or collidable wall.</summary>

[ObjectType("Wall")]
public partial class Wall : ScaledObjectDefinition
{
    /// <summary>The wall shape key.</summary>
    [InspectorField("Shape", Order = 2.5f, Options = "Shapes")]
    public string Shape = "Box";

    /// <summary>The wall material key.</summary>
    [InspectorField("Material", Order = 3, Options = "Materials")]
    public string Material = "Brick";

    /// <summary>Whether the material will be kept stable for rotation and movement or stable in world space.</summary>
    [InspectorField("Stable Material", Order = 3.5f)]
    public bool StableMaterial = false;

    /// <summary>The velocity multiplier applied on contact.</summary>
    [InspectorField("Damp", Order = 4, Min = 0f, Max = 1f, Slider = true)]
    public float Damp = 1f;

    /// <summary>Whether the wall has visuals, collision, or both.</summary>
    [InspectorField("Mode", Order = 5)]
    public WallMode Mode = WallMode.VisualsCollision;

    /// <summary>Whether touching the wall kills the player.</summary>
    [InspectorField("Deadly", Order = 5.5f)]
    public bool Deadly = false;

    /// <summary>The impact velocity required to break the wall, or a negative value to disable breaking.</summary>
    [InspectorField("Break Velocity", Order = 6, Min = -1f, Max = 999f)]
    public float BreakVelocity = -1f;

    /// <summary>The objects triggered when the wall breaks.</summary>
    [InspectorField("On Break", Order = 8)]
    public ObjectReferences Trigger = new();

    /// <summary>The objects triggered when the player touches the wall.</summary>
    [InspectorField("On Touch", Order = 9)]
    public ObjectReferences OnTouch = new();

    /// <summary>The minimum time in seconds between touch activations.</summary>
    [InspectorField("Touch Cooldown", Order = 10, Min = 0f, Max = 999f)]
    public float TouchCooldown = 0f;
}
/// <summary>Defines a point light.</summary>

[ObjectType("Light")]
public sealed partial class LightObjectDefinition : PhysicalObjectDefinition
{
    /// <summary>The light red channel in the range 0 to 1.</summary>
    [InspectorField("R", Order = 3, Min = 0f, Max = 1f, Slider = true)]
    public float Red = 1f;

    /// <summary>The light green channel in the range 0 to 1.</summary>
    [InspectorField("G", Order = 4, Min = 0f, Max = 1f, Slider = true)]
    public float Green = 1f;

    /// <summary>The light blue channel in the range 0 to 1.</summary>
    [InspectorField("B", Order = 5, Min = 0f, Max = 1f, Slider = true)]
    public float Blue = 1f;

    /// <summary>The light intensity.</summary>
    [InspectorField("Intensity", Order = 6, Min = 0f, Max = 999f)]
    public float Intensity = 1f;

    /// <summary>The light radius.</summary>
    [InspectorField("Radius", Order = 7, Min = 0f, Max = 99f)]
    public float Radius = 10f;
}

/// <summary>Defines a box-shaped magnetic field.</summary>

[ObjectType("Magnet Field")]
public sealed partial class MagnetFieldObjectDefinition : ScaledObjectDefinition
{

    /// <summary>The magnetic force strength.</summary>
    [InspectorField("Strength", Order = 3, Min = -999f, Max = 999f)]
    public float Strength = 25f;
}

/// <summary>Defines a checkpoint.</summary>
[ObjectType("Checkpoint")]
public sealed partial class CheckpointObjectDefinition : UniformScaledObjectDefinition
{

    /// <summary>The direction the player faces after respawning.</summary>
    [InspectorField("Direction", Order = 3)]
    public CardinalDirection Direction;

    /// <summary>Whether the checkpoint is rendered.</summary>
    [InspectorField("Visible", Order = 4)]
    public bool Visible = true;
    /// <summary>Whether reaching the checkpoint plays a sound.</summary>
    [InspectorField("Audible", Order = 4.5f)]
    public bool Audible = true;
    /// <summary>The objects triggered when the checkpoint is reached.</summary>
    [InspectorField("On Hit", Order = 6)]
    public ObjectReferences Trigger = new();
}

/// <summary>Defines the level completion target.</summary>

[ObjectType("Win")]
public sealed partial class WinObjectDefinition : UniformScaledObjectDefinition
{
    /// <summary>Whether the win target is rendered.</summary>
    [InspectorField("Visible", Order = 3)]
    public bool Visible = true;

    /// <summary>Whether reaching the win target plays a sound.</summary>
    [InspectorField("Audible", Order = 3.5f)]
    public bool Audible = true;

    /// <summary>The objects triggered when the level is completed.</summary>
    [InspectorField("On Win", Order = 4)]
    public ObjectReferences Trigger = new();
}

/// <summary>Defines world-space text.</summary>

[ObjectType("Text")]
public sealed partial class TextObjectDefinition : ScaledObjectDefinition
{

    /// <summary>The text width for word wrapping.</summary>
    [InspectorField("Width", Order = 3, Min = 0.1f, Max = 999f)]
    public float Width = 3f;

    /// <summary>The text red channel in the range 0 to 1.</summary>
    [InspectorField("R", Order = 3.1f, Min = 0f, Max = 1f, Slider = true)]
    public float Red = 1f;

    /// <summary>The text green channel in the range 0 to 1.</summary>
    [InspectorField("G", Order = 3.2f, Min = 0f, Max = 1f, Slider = true)]
    public float Green = 1f;

    /// <summary>The text blue channel in the range 0 to 1.</summary>
    [InspectorField("B", Order = 3.3f, Min = 0f, Max = 1f, Slider = true)]
    public float Blue = 1f;

    /// <summary>The font size.</summary>
    [InspectorField("Font Size", Order = 4, Min = 0.1f, Max = 999f)]
    public float FontSize = 10f;

    /// <summary>The displayed text.</summary>
    [InspectorField("Text", Order = 5, Multiline = true)]
    public string Text = "Text";
    [InspectorField("Counter ID", Order = 6, NoInspect = true)]
    internal int CounterID = -1;
}

/// <summary>Defines a spherical magnetic force point.</summary>

[ObjectType("Magnet Point")]
public sealed partial class MagnetPointObjectDefinition : PhysicalObjectDefinition
{
    /// <summary>The magnetic force strength; negative values repel.</summary>
    [InspectorField("Strength", Order = 3, Min = -999f, Max = 999f)]
    public float Strength = 25f;

    /// <summary>The magnetic effect radius.</summary>
    [InspectorField("Radius", Order = 4, Min = 0f, Max = 999f)]
    public float Radius = 10f;
}


/// <summary>Moves referenced objects when triggered.</summary>
[ObjectType("Move Trigger")]
public sealed partial class MoveTriggerObjectDefinition : PhysicalObjectDefinition
{
    /// <summary>The object to move.</summary>
    [InspectorField("Move", Order = 3.5f)]
    public ObjectReferences Move = new();

    /// <summary>How <see cref="MoveSpeed"/> is interpreted.</summary>
    [InspectorField("Move Speed Mode", Order = 4)]
    public TriggerSpeedMode MoveSpeedMode = TriggerSpeedMode.WorldSpeed;

    /// <summary>The movement speed or total duration.</summary>
    [InspectorField("Move Speed", Order = 5, Min = 0f, Max = 999f)]
    public float MoveSpeed = 1f;

    /// <summary>The movement easing curve.</summary>
    [InspectorField("Easing Type", Order = 5.5f)]
    public EasingType EasingType = EasingType.Linear;

    /// <summary>The movement easing direction.</summary>
    [InspectorField("Easing Direction", Order = 5.6f)]
    public EasingDirection EasingDirection = EasingDirection.In;

    /// <summary>The objects triggered when movement finishes.</summary>
    [InspectorField("On Done", Order = 7)]
    public ObjectReferences Trigger = new();
}

/// <summary>Rotates referenced objects when triggered.</summary>

[ObjectType("Rotation Trigger")]
public sealed partial class RotationTriggerObjectDefinition : PhysicalObjectDefinition
{
    /// <summary>The object to rotate.</summary>
    [InspectorField("Rotate", Order = 3.5f)]
    public ObjectReferences Rotate = new();

    /// <summary>How <see cref="RotationSpeed"/> is interpreted.</summary>
    [InspectorField("Rotation Speed Mode", Order = 4)]
    public TriggerSpeedMode RotationSpeedMode = TriggerSpeedMode.WorldSpeed;

    /// <summary>The rotation speed or total duration.</summary>
    [InspectorField("Rotation Speed", Order = 5, Min = 0f, Max = 999f)]
    public float RotationSpeed = 90f;

    /// <summary>The rotation easing curve.</summary>
    [InspectorField("Easing Type", Order = 5.5f)]
    public EasingType EasingType = EasingType.Linear;

    /// <summary>The rotation easing direction.</summary>
    [InspectorField("Easing Direction", Order = 5.6f)]
    public EasingDirection EasingDirection = EasingDirection.InOut;

    /// <summary>The objects triggered when rotation finishes.</summary>
    [InspectorField("On Done", Order = 7)]
    public ObjectReferences Trigger = new();
}

/// <summary>Teleports the player to this object's position when triggered.</summary>

[ObjectType("Teleport Trigger")]
public sealed partial class TeleportTriggerObjectDefinition : PhysicalObjectDefinition
{
    /// <summary>The objects triggered after teleporting.</summary>
    [InspectorField("On Done", Order = 3)]
    public ObjectReferences Trigger = new();
}

/// <summary>Kills the player when triggered.</summary>

[ObjectType("Death Trigger")]
public sealed partial class DeathTriggerObjectDefinition : ObjectDefinition
{
    /// <summary>The objects triggered whenever the player respawns.</summary>
    [InspectorField("On Respawn", Order = 3)]
    public ObjectReferences OnRespawn = new();
}

/// <summary>Makes referenced objects follow the player or camera.</summary>

[ObjectType("Follow Trigger")]
public sealed partial class FollowTriggerObjectDefinition : ObjectDefinition
{
    /// <summary>The transform that referenced objects follow.</summary>
    [InspectorField("Follow Target", Order = 3)]
    public FollowTarget FollowTarget = FollowTarget.Player;

    /// <summary>The interpolation rate toward the target.</summary>
    [InspectorField("Follow Rate", Order = 4, Min = 0f, Max = 1000f)]
    public float FollowRate = 3f;

    /// <summary>Whether referenced objects snap to the target immediately.</summary>
    [InspectorField("Snap", Order = 4.5f)]
    public bool Snap = false;

    /// <summary>The object that follows the target.</summary>
    [InspectorField("Follow", Order = 5)]
    public ObjectReferences Follow = new();

    /// <summary>Whether following begins when the level starts.</summary>
    [InspectorField("Run On Start", Order = 6)]
    public bool RunOnStart = false;
}

/// <summary>Destroys referenced objects when triggered.</summary>
[ObjectType("Destroy Trigger")]
public sealed partial class DestroyTriggerObjectDefinition : ObjectDefinition
{

    /// <summary>The objects to destroy.</summary>
    [InspectorField("Destroy", Order = 3.5f)]
    public ObjectReferences Destroy = new();

    /// <summary>The delay in seconds before destruction.</summary>
    [InspectorField("Wait", Order = 6, Min = 0f, Max = 999f)]
    public float Wait = 0f;
}

/// <summary>Triggers referenced objects when the player touches its volume.</summary>

[ObjectType("Touch Trigger")]
public sealed partial class TouchTriggerObjectDefinition : UniformScaledObjectDefinition
{

    /// <summary>The objects triggered on contact.</summary>
    [InspectorField("Trigger", Order = 3.5f)]
    public ObjectReferences Trigger = new();

    /// <summary>The minimum time in seconds between activations, or a negative value for one use.</summary>
    [InspectorField("Cooldown", Order = 4, Min = -1f, Max = 999f)]
    public float Cooldown = 0f;
}

/// <summary>Triggers referenced objects after a delay.</summary>

[ObjectType("Delay Trigger")]
public sealed partial class DelayTriggerObjectDefinition : ObjectDefinition
{
    /// <summary>The objects triggered after the delay.</summary>
    [InspectorField("Trigger", Order = 3.5f)]
    public ObjectReferences Trigger = new();

    /// <summary>The delay in seconds.</summary>
    [InspectorField("Wait", Order = 4, Min = 0f, Max = 999f)]
    public float Wait = 0f;

    /// <summary>Whether the delay begins when the level starts.</summary>
    [InspectorField("Run On Start", Order = 5)]
    public bool RunOnStart = false;
}

/// <summary>Applies an arithmetic operation to a named runtime counter.</summary>

[ObjectType("Counter Trigger")]
public sealed partial class CounterTriggerObjectDefinition : ObjectDefinition
{
    /// <summary>The counter to operate on.</summary>
    [InspectorField("Counter", Order = 3)]
    public string Counter;
    [InspectorField("Counter ID", Order = 4, NoInspect = true)]
    internal int CounterID = -1;
    /// <summary>The arithmetic operation.</summary>
    [InspectorField("Operator", Order = 5)]
    public Operator Operator = Operator.Set;
    /// <summary>The operand, optionally containing a counter expression.</summary>
    [InspectorField("Operand", Order = 6)]
    public string Operand = "1";
    [InspectorField("Counter IDs", Order = 7, NoInspect = true)]
    internal int[] CounterIDs = [];
}

/// <summary>Triggers one of two object sets based on an expression.</summary>

[ObjectType("If Trigger")]
public sealed partial class IfTriggerObjectDefinition : ObjectDefinition
{

    /// <summary>The counter expression, if != 0 it is true, if == 0 it is false.</summary>
    [InspectorField("Expression", Order = 3.5f)]
    public string Expression = "1";
    [InspectorField("Counter IDs", Order = 4.5f, NoInspect = true)]
    internal int[] CounterIDs = [];

    [InspectorField("Counter", Order = 3, Old = true)]
    internal string Counter = "";

    [InspectorField("Operand", Order = 4, Old = true)]
    internal int Operand = 1;

    [InspectorField("Comparison", Order = 5, Old = true)]
    internal Comparison Operator = Comparison.Equal;

    /// <summary>The objects triggered when the comparison succeeds.</summary>
    [InspectorField("True Trigger", Order = 8)]
    public ObjectReferences TrueTrigger = new();

    /// <summary>The objects triggered when the comparison fails.</summary>
    [InspectorField("False Trigger", Order = 9)]
    public ObjectReferences FalseTrigger = new();
}

/// <summary>Changes the material of referenced walls.</summary>

[ObjectType("Set Material Trigger")]
public sealed partial class SetMaterialTriggerObjectDefinition : ObjectDefinition
{
    /// <summary>The material key to apply.</summary>
    [InspectorField("Material", Order = 3, Options = "Materials")]
    public string Material = "Brick";


    /// <summary>The walls whose material is changed.</summary>
    [InspectorField("Wall", Order = 5)]
    public ObjectReferences Wall = new();
}

/// <summary>Changes the sky colour and fog intensity.</summary>

[ObjectType("Sky Colour Trigger")]
public sealed partial class SkyColourTriggerObjectDefinition : ObjectDefinition
{
    /// <summary>The sky red channel in the range 0 to 1.</summary>
    [InspectorField("R", Order = 3, Min = 0f, Max = 1f, Slider = true)]
    public float Red = 1f;

    /// <summary>The sky green channel in the range 0 to 1.</summary>
    [InspectorField("G", Order = 4, Min = 0f, Max = 1f, Slider = true)]
    public float Green = 1f;

    /// <summary>The sky blue channel in the range 0 to 1.</summary>
    [InspectorField("B", Order = 5, Min = 0f, Max = 1f, Slider = true)]
    public float Blue = 1f;

    /// <summary>The target fog intensity.</summary>
    [InspectorField("Fog Intensity", Order = 6, Min = 0, Max = 20, Slider = true)]
    public int FogIntensity = 3;
}

/// <summary>Enables, disables, or flips referenced objects.</summary>

[ObjectType("Toggle Trigger")]
public sealed partial class ToggleTriggerObjectDefinition : ObjectDefinition
{

    /// <summary>The objects whose state is changed.</summary>
    [InspectorField("Toggle", Order = 3.5f)]
    public ObjectReferences Toggle = new();

    /// <summary>The state change to apply.</summary>
    [InspectorField("Mode", Order = 4)]
    public ToggleMode Mode = ToggleMode.On;
}


/// <summary>Defines a collectible coin.</summary>

[ObjectType("Coin")]
public sealed partial class CoinObjectDefinition : UniformScaledObjectDefinition
{
    /// <summary>Whether the coin is rendered.</summary>
    [InspectorField("Visible", Order = 2.5f)]
    public bool Visible = true;
    /// <summary>Whether collecting the coin plays a sound.</summary>
    [InspectorField("Audible", Order = 2.6f)]
    public bool Audible = true;

    /// <summary>The objects triggered when the coin is collected.</summary>
    [InspectorField("On Pickup", Order = 4)]
    public ObjectReferences OnPickup = new();
}
#endif
