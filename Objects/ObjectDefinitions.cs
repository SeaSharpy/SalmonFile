namespace Salmon.Levels.Objects;

/// <summary>Provides identity data used by every level object.</summary>
public abstract class ObjectDefinition
{
    /// <summary>The object name, unique within its parent group.</summary>
    [InspectorField("Name", Order = 0, OldOrder = -999, ID = 0)]
    public string Name = "Unknown";

    /// <summary>The persistent object ID.</summary>
    [InspectorField(Order = 1, OldOrder = -1, ID = 1, NoInspect = true)]
    public ulong ID { get; internal set; } = Snowflake.CreateULong();
}


/// <summary>Adds local position, rotation and a Unity GameObject to a level object.</summary>
public abstract partial class PhysicalObjectDefinition : ObjectDefinition
{
    /// <summary>The local position.</summary>
    [InspectorField("Position", Order = 2, OldOrder = 0, ID = 2, Handle = InspectorHandleType.Move)]
    public Vector3 Position = Vector3.zero;

    /// <summary>The local Euler rotation in degrees.</summary>
    [InspectorField("Rotation", Order = 3, OldOrder = 1, ID = 3, Handle = InspectorHandleType.Rotate)]
    public Vector3 Rotation = Vector3.zero;
}

/// <summary>Stores a set of persistent level object IDs.</summary>
public sealed partial class ObjectReferences() : ISpecialSerializable
{
    /// <summary>Creates an object references with some IDs.</summary>
    public ObjectReferences(params ulong[] ids) : this() => IDs.UnionWith(ids);
    /// <summary>
    /// The referenced object identifiers. 
    /// More than 8 will cause strange UI glitches and more than 255 will not save correctly. 
    /// </summary>
    public readonly HashSet<ulong> IDs = new(8);
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
    /// <exclude/>
    public void SpecialWrite(BinaryWriter writer)
    {
        writer.Write((byte)IDs.Count);
        foreach (var id in IDs)
            writer.Write(id);
    }
    /// <exclude/>
    public void SpecialRead(BinaryReader reader)
    {
        byte count = reader.ReadByte();
        IDs.Clear();
        for (var i = 0; i < count; i++)
            IDs.Add(reader.ReadUInt64());
    }
}

/// <summary>Adds non-uniform local scale to a physical level object.</summary>
public abstract partial class ScaledObjectDefinition : PhysicalObjectDefinition
{
    /// <summary>The local scale on each axis.</summary>
    [InspectorField("Scale", Order = 4, OldOrder = 2, ID = 4, Min = 0.05f, Handle = InspectorHandleType.Scale)]
    public Vector3 Scale = Vector3.one;
}

/// <summary>Adds uniform local scale to a physical level object.</summary>
public abstract partial class UniformScaledObjectDefinition : PhysicalObjectDefinition
{
    /// <summary>The uniform local scale.</summary>
    [InspectorField("Scale", Order = 4, OldOrder = 2, ID = 4, Min = 0.05f, Handle = InspectorHandleType.Scale)]
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
    /// <exclude/>
    public void SpecialWrite(BinaryWriter writer) => SerializeObjects(writer, Objects);
    /// <exclude/>
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
            var obj = DynamicSerializer.Deserialize(type, reader);
            Objects.Add((ObjectDefinition)obj);
        }
    }

    internal static void SerializeObjects(BinaryWriter writer, List<ObjectDefinition> objects)
    {
        writer.Write(objects.Count);
        foreach (var obj in objects)
        {
            writer.Write(obj.GetDisplayBytes());
            DynamicSerializer.Serialize(obj, writer);
        }
    }
}

/// <summary>Defines a builtin prefab put in the level.</summary>
[ObjectType("Builtin")]
public partial class Builtin : ScaledObjectDefinition
{
    /// <summary>Which prefab to put in the level.</summary>
    [InspectorField("Target", Order = 5, OldOrder = 3, ID = 5, Options = "Prefabs")]
    public string Target = "";
}

/// <summary>Defines a rendered and/or collidable wall.</summary>
[ObjectType("Wall")]
public partial class Wall : ScaledObjectDefinition
{
    /// <summary>The wall shape key.</summary>
    [InspectorField("Shape", Order = 5, OldOrder = 2.5f, ID = 5, Options = "Shapes")]
    public string Shape = "Box";

    /// <summary>The wall material key.</summary>
    [InspectorField("Material", Order = 6, OldOrder = 3, ID = 6, Options = "Materials")]
    public string Material = "Brick";

    /// <summary>Whether the material will be kept stable for rotation and movement or stable in world space.</summary>
    [InspectorField("Stable Material", Order = 7, OldOrder = 3.5f, ID = 7)]
    public bool StableMaterial = false;

    /// <summary>The velocity multiplier applied on contact.</summary>
    [InspectorField("Damp", Order = 8, OldOrder = 4, ID = 8, Min = 0f, Max = 1f, Slider = true)]
    public float Damp = 1f;

    /// <summary>Whether the wall has visuals, collision, or both.</summary>
    [InspectorField("Mode", Order = 9, OldOrder = 5, ID = 9)]
    public WallMode Mode = WallMode.VisualsCollision;

    /// <summary>Whether touching the wall kills the player.</summary>
    [InspectorField("Deadly", Order = 10, OldOrder = 5.5f, ID = 10)]
    public bool Deadly = false;

    /// <summary>The impact velocity required to break the wall, or a negative value to disable breaking.</summary>
    [InspectorField("Break Velocity", Order = 11, OldOrder = 6, ID = 11, Min = -1f, Max = 999f)]
    public float BreakVelocity = -1f;

    /// <summary>The objects triggered when the wall breaks.</summary>
    [InspectorField("On Break", Order = 12, OldOrder = 8, ID = 12)]
    public ObjectReferences Trigger = new();

    /// <summary>The objects triggered when the player touches the wall.</summary>
    [InspectorField("On Touch", Order = 13, OldOrder = 9, ID = 13)]
    public ObjectReferences OnTouch = new();

    /// <summary>The minimum time in seconds between touch activations.</summary>
    [InspectorField("Touch Cooldown", Order = 14, OldOrder = 10, ID = 14, Min = 0f, Max = 999f)]
    public float TouchCooldown = 0f;
}
/// <summary>Defines a point light.</summary>

[ObjectType("Light")]
public sealed partial class LightObjectDefinition : PhysicalObjectDefinition
{
    /// <summary>The light red channel in the range 0 to 1.</summary>
    [InspectorField("R", Order = 4, OldOrder = 3, ID = 4, Min = 0f, Max = 1f, Slider = true)]
    public float Red = 1f;

    /// <summary>The light green channel in the range 0 to 1.</summary>
    [InspectorField("G", Order = 5, OldOrder = 4, ID = 5, Min = 0f, Max = 1f, Slider = true)]
    public float Green = 1f;

    /// <summary>The light blue channel in the range 0 to 1.</summary>
    [InspectorField("B", Order = 6, OldOrder = 5, ID = 6, Min = 0f, Max = 1f, Slider = true)]
    public float Blue = 1f;

    /// <summary>The light intensity.</summary>
    [InspectorField("Intensity", Order = 7, OldOrder = 6, ID = 7, Min = 0f, Max = 999f)]
    public float Intensity = 1f;

    /// <summary>The light radius.</summary>
    [InspectorField("Radius", Order = 8, OldOrder = 7, ID = 8, Min = 0f, Max = 99f)]
    public float Radius = 10f;
}

/// <summary>Defines a box-shaped magnetic field.</summary>

[ObjectType("Magnet Field")]
public sealed partial class MagnetFieldObjectDefinition : ScaledObjectDefinition
{

    /// <summary>The magnetic force strength.</summary>
    [InspectorField("Strength", Order = 5, OldOrder = 3, ID = 5, Min = -999f, Max = 999f)]
    public float Strength = 25f;
}

/// <summary>Spoiler alert!!! Not out yet.</summary>
[ObjectType("Water")]
public sealed partial class WaterObjectDefinition : ScaledObjectDefinition
{
    [InspectorField("Horizontal Drag", Order = 5, OldOrder = 3, ID = 5, Min = 0f, Max = 999f)]
    public float HorizontalDrag = 0.25f;
    [InspectorField("Vertical Drag", Order = 6, OldOrder = 4, ID = 6, Min = 0f, Max = 999f)]
    public float VerticalDrag = 0.5f;
}

/// <summary>Defines a checkpoint.</summary>
[ObjectType("Checkpoint")]
public sealed partial class CheckpointObjectDefinition : UniformScaledObjectDefinition
{

    /// <summary>The direction the player faces after respawning.</summary>
    [InspectorField("Direction", Order = 5, OldOrder = 3, ID = 5)]
    public CardinalDirection Direction;
    /// <summary>Whether the checkpoint is rendered.</summary>
    [InspectorField("Visible", Order = 6, OldOrder = 4, ID = 6)]
    public bool Visible = true;
    /// <summary>Whether hitting the checkpoint plays a sound.</summary>
    [InspectorField("Audible", Order = 7, OldOrder = 4.5f, ID = 7)]
    public bool Audible = true;
    /// <summary>The objects triggered when the checkpoint is hit.</summary>
    [InspectorField("On Hit", Order = 8, OldOrder = 6, ID = 8)]
    public ObjectReferences Trigger = new();
}

/// <summary>Defines a win.</summary>
[ObjectType("Win")]
public sealed partial class WinObjectDefinition : UniformScaledObjectDefinition
{
    /// <summary>Whether the win is rendered.</summary>
    [InspectorField("Visible", Order = 5, OldOrder = 3, ID = 5)]
    public bool Visible = true;

    /// <summary>Whether winning the win plays a sound.</summary>
    [InspectorField("Audible", Order = 6, OldOrder = 3.5f, ID = 6)]
    public bool Audible = true;

    /// <summary>The objects triggered when the win is won.</summary>
    [InspectorField("On Win", Order = 7, OldOrder = 4, ID = 7)]
    public ObjectReferences Trigger = new();
}

/// <summary>Defines world-space text.</summary>
[ObjectType("Text")]
public sealed partial class TextObjectDefinition : ScaledObjectDefinition
{

    /// <summary>The text width for word wrapping.</summary>
    [InspectorField("Width", Order = 5, OldOrder = 3, ID = 5, Min = 0.1f, Max = 999f)]
    public float Width = 3f;

    /// <summary>The text red channel in the range 0 to 1.</summary>
    [InspectorField("R", Order = 6, OldOrder = 3.1f, ID = 6, Min = 0f, Max = 1f, Slider = true)]
    public float Red = 1f;

    /// <summary>The text green channel in the range 0 to 1.</summary>
    [InspectorField("G", Order = 7, OldOrder = 3.2f, ID = 7, Min = 0f, Max = 1f, Slider = true)]
    public float Green = 1f;

    /// <summary>The text blue channel in the range 0 to 1.</summary>
    [InspectorField("B", Order = 8, OldOrder = 3.3f, ID = 8, Min = 0f, Max = 1f, Slider = true)]
    public float Blue = 1f;

    /// <summary>The font size.</summary>
    [InspectorField("Font Size", Order = 9, OldOrder = 4, ID = 9, Min = 0.1f, Max = 999f)]
    public float FontSize = 10f;

    /// <summary>The displayed text.</summary>
    [InspectorField("Text", Order = 10, OldOrder = 5, ID = 10, Multiline = true)]
    public string Text = "Text";
    [InspectorField(Order = 11, OldOrder = 6, ID = 11, NoInspect = true)]
    internal int CounterID = -1;
}

/// <summary>Defines a spherical magnetic force point.</summary>
[ObjectType("Magnet Point")]
public sealed partial class MagnetPointObjectDefinition : PhysicalObjectDefinition
{
    /// <summary>The magnetic force strength. Negative values repel.</summary>
    [InspectorField("Strength", Order = 4, OldOrder = 3, ID = 4, Min = -999f, Max = 999f)]
    public float Strength = 25f;

    /// <summary>The magnetic effect radius.</summary>
    [InspectorField("Radius", Order = 5, OldOrder = 4, ID = 5, Min = 0f, Max = 999f)]
    public float Radius = 10f;
}


/// <summary>Moves referenced objects when triggered.</summary>
[ObjectType("Move Trigger")]
public sealed partial class MoveTriggerObjectDefinition : PhysicalObjectDefinition
{
    /// <summary>The object to move (only one).</summary>
    [InspectorField("Move", Order = 4, OldOrder = 3.5f, ID = 4)]
    public ObjectReferences Move = new();

    /// <summary>How <see cref="MoveSpeed"/> is interpreted.</summary>
    [InspectorField("Move Speed Mode", Order = 5, OldOrder = 4, ID = 5)]
    public TriggerSpeedMode MoveSpeedMode = TriggerSpeedMode.WorldSpeed;

    /// <summary>The movement speed or total duration.</summary>
    [InspectorField("Move Speed", Order = 6, OldOrder = 5, ID = 6, Min = 0f, Max = 999f)]
    public float MoveSpeed = 1f;

    /// <summary>The movement easing curve.</summary>
    [InspectorField("Easing Type", Order = 7, OldOrder = 5.5f, ID = 7)]
    public EasingType EasingType = EasingType.Linear;

    /// <summary>The movement easing direction.</summary>
    [InspectorField("Easing Direction", Order = 8, OldOrder = 5.6f, ID = 8)]
    public EasingDirection EasingDirection = EasingDirection.In;

    /// <summary>The objects triggered when movement finishes.</summary>
    [InspectorField("On Done", Order = 9, OldOrder = 7, ID = 9)]
    public ObjectReferences Trigger = new();
}

/// <summary>Rotates referenced objects when triggered.</summary>
[ObjectType("Rotation Trigger")]
public sealed partial class RotationTriggerObjectDefinition : PhysicalObjectDefinition
{
    /// <summary>The object to rotate (only one).</summary>
    [InspectorField("Rotate", Order = 4, OldOrder = 3.5f, ID = 4)]
    public ObjectReferences Rotate = new();

    /// <summary>How <see cref="RotationSpeed"/> is interpreted.</summary>
    [InspectorField("Rotation Speed Mode", Order = 5, OldOrder = 4, ID = 5)]
    public TriggerSpeedMode RotationSpeedMode = TriggerSpeedMode.WorldSpeed;

    /// <summary>The rotation speed or total duration.</summary>
    [InspectorField("Rotation Speed", Order = 6, OldOrder = 5, ID = 6, Min = 0f, Max = 999f)]
    public float RotationSpeed = 90f;

    /// <summary>The rotation easing curve.</summary>
    [InspectorField("Easing Type", Order = 7, OldOrder = 5.5f, ID = 7)]
    public EasingType EasingType = EasingType.Linear;

    /// <summary>The rotation easing direction.</summary>
    [InspectorField("Easing Direction", Order = 8, OldOrder = 5.6f, ID = 8)]
    public EasingDirection EasingDirection = EasingDirection.InOut;

    /// <summary>The objects triggered when rotation finishes.</summary>
    [InspectorField("On Done", Order = 9, OldOrder = 7, ID = 9)]
    public ObjectReferences Trigger = new();
}

/// <summary>Teleports the player to this object's position when triggered.</summary>
[ObjectType("Teleport Trigger")]
public sealed partial class TeleportTriggerObjectDefinition : PhysicalObjectDefinition
{
    /// <summary>The objects triggered after teleporting.</summary>
    [InspectorField("On Done", Order = 4, OldOrder = 3, ID = 4)]
    public ObjectReferences Trigger = new();
}

/// <summary>Kills the player when triggered.</summary>
[ObjectType("Death Trigger")]
public sealed partial class DeathTriggerObjectDefinition : ObjectDefinition
{
    /// <summary>The objects triggered whenever the player respawns (for any reason).</summary>
    [InspectorField("On Respawn", Order = 2, OldOrder = 3, ID = 2)]
    public ObjectReferences OnRespawn = new();
}

/// <summary>Makes referenced object follow the player or camera.</summary>
[ObjectType("Follow Trigger")]
public sealed partial class FollowTriggerObjectDefinition : ObjectDefinition
{
    /// <summary>The transform that the referenced object follow.</summary>
    [InspectorField("Follow Target", Order = 2, OldOrder = 3, ID = 2)]
    public FollowTarget FollowTarget = FollowTarget.Player;

    /// <summary>The interpolation rate toward the target.</summary>
    [InspectorField("Follow Rate", Order = 3, OldOrder = 4, ID = 3, Min = 0f, Max = 1000f)]
    public float FollowRate = 3f;

    /// <summary>Whether referenced objects snap to the target immediately.</summary>
    [InspectorField("Snap", Order = 4, OldOrder = 4.5f, ID = 4)]
    public bool Snap = false;

    /// <summary>The object that follows the target (only one).</summary>
    [InspectorField("Follow", Order = 5, OldOrder = 5, ID = 5)]
    public ObjectReferences Follow = new();

    /// <summary>Whether following begins when the level starts.</summary>
    [InspectorField("Run On Start", Order = 6, OldOrder = 6, ID = 6)]
    public bool RunOnStart = false;
}

/// <summary>Destroys referenced objects when triggered.</summary>
[ObjectType("Destroy Trigger")]
public sealed partial class DestroyTriggerObjectDefinition : ObjectDefinition
{
    /// <summary>The objects to destroy.</summary>
    [InspectorField("Destroy", Order = 2, OldOrder = 3.5f, ID = 2)]
    public ObjectReferences Destroy = new();

    /// <summary>The delay in seconds before destruction.</summary>
    [InspectorField("Wait", Order = 3, OldOrder = 6, ID = 3, Min = 0f, Max = 999f)]
    public float Wait = 0f;
}

/// <summary>Triggers referenced objects when the player touches its sphere.</summary>
[ObjectType("Touch Trigger")]
public sealed partial class TouchTriggerObjectDefinition : UniformScaledObjectDefinition
{
    /// <summary>The objects triggered on hit.</summary>
    [InspectorField("Trigger", Order = 5, OldOrder = 3.5f, ID = 5)]
    public ObjectReferences Trigger = new();

    /// <summary>The minimum time in seconds between activations, or a negative value for single use.</summary>
    [InspectorField("Cooldown", Order = 6, OldOrder = 4, ID = 6, Min = -1f, Max = 999f)]
    public float Cooldown = 0f;
}

/// <summary>Triggers referenced objects after a delay.</summary>
[ObjectType("Delay Trigger")]
public sealed partial class DelayTriggerObjectDefinition : ObjectDefinition
{
    /// <summary>The objects triggered after the delay.</summary>
    [InspectorField("Trigger", Order = 2, OldOrder = 3.5f, ID = 2)]
    public ObjectReferences Trigger = new();

    /// <summary>The delay in seconds.</summary>
    [InspectorField("Wait", Order = 3, OldOrder = 4, ID = 3, Min = 0f, Max = 999f)]
    public float Wait = 0f;

    /// <summary>Whether the delay begins when the level starts.</summary>
    [InspectorField("Run On Start", Order = 4, OldOrder = 5, ID = 4)]
    public bool RunOnStart = false;
}

/// <summary>Applies an arithmetic operation to a named counter.</summary>
[ObjectType("Counter Trigger")]
public sealed partial class CounterTriggerObjectDefinition : ObjectDefinition
{
    /// <summary>The counter to operate on.</summary>
    [InspectorField("Counter", Order = 2, OldOrder = 3, ID = 2)]
    public string Counter;
    [InspectorField(Order = 3, OldOrder = 4, ID = 3, NoInspect = true)]
    internal int CounterID = -1;
    /// <summary>The operation.</summary>
    [InspectorField("Operator", Order = 4, OldOrder = 5, ID = 4)]
    public Operator Operator = Operator.Set;
    /// <summary>
    /// The operand, optionally containing a counter expression .
    /// Just do any maths really with $ for other counters, these are 32 bit integers.
    /// You can even do ternary and bitwise operations.
    /// </summary>
    [InspectorField("Operand", Order = 5, OldOrder = 6, ID = 5)]
    public string Operand = "1";
    [InspectorField(Order = 6, OldOrder = 7, ID = 6, NoInspect = true)]
    internal int[] CounterIDs = [];
}

/// <summary>Triggers one of two object sets based on an expression.</summary>
[ObjectType("If Trigger")]
public sealed partial class IfTriggerObjectDefinition : ObjectDefinition
{
    /// <summary>
    /// The counter expression, if != 0 it is true, if == 0 it is false.
    /// See <see cref="CounterTriggerObjectDefinition.Operand"/> for a short tutorial.
    /// </summary>
    [InspectorField("Expression", Order = 3, OldOrder = 3.5f, ID = 3)]
    public string Expression = "1";
    [InspectorField(Order = 5, OldOrder = 4.5f, ID = 5, NoInspect = true)]
    internal int[] CounterIDs = [];

    [InspectorField(Order = 2, OldOrder = 3, ID = 2, Old = true)]
    internal string Counter = "";

    [InspectorField(Order = 4, OldOrder = 4, ID = 4, Old = true)]
    internal int Operand = 1;

    [InspectorField(Order = 6, OldOrder = 5, ID = 6, Old = true)]
    internal Comparison Operator = Comparison.Equal;

    /// <summary>The objects triggered when the comparison succeeds.</summary>
    [InspectorField("True Trigger", Order = 7, OldOrder = 8, ID = 7)]
    public ObjectReferences TrueTrigger = new();

    /// <summary>The objects triggered when the comparison fails.</summary>
    [InspectorField("False Trigger", Order = 8, OldOrder = 9, ID = 8)]
    public ObjectReferences FalseTrigger = new();
}

/// <summary>Triggers a random one of the object references.</summary>
[ObjectType("Random Trigger")]
public sealed partial class RandomTriggerObjectDefinition : ObjectDefinition
{
    /// <summary>The objects which will have one triggered.</summary>
    [InspectorField("Trigger", Order = 2, OldOrder = 3.5f, ID = 2)]
    public ObjectReferences Trigger = new();
}

/// <summary>Changes the material of referenced walls.</summary>
[ObjectType("Set Material Trigger")]
public sealed partial class SetMaterialTriggerObjectDefinition : ObjectDefinition
{
    /// <summary>The material key to apply.</summary>
    [InspectorField("Material", Order = 2, OldOrder = 3, ID = 2, Options = "Materials")]
    public string Material = "Brick";

    /// <summary>The walls whose material is changed.</summary>
    [InspectorField("Wall", Order = 3, OldOrder = 5, ID = 3)]
    public ObjectReferences Wall = new();
}

/// <summary>Changes the sky colour and fog intensity.</summary>
[ObjectType("Sky Colour Trigger")]
public sealed partial class SkyColourTriggerObjectDefinition : ObjectDefinition
{
    /// <summary>The sky red channel in the range 0 to 1.</summary>
    [InspectorField("R", Order = 2, OldOrder = 3, ID = 2, Min = 0f, Max = 1f, Slider = true)]
    public float Red = 1f;

    /// <summary>The sky green channel in the range 0 to 1.</summary>
    [InspectorField("G", Order = 3, OldOrder = 4, ID = 3, Min = 0f, Max = 1f, Slider = true)]
    public float Green = 1f;

    /// <summary>The sky blue channel in the range 0 to 1.</summary>
    [InspectorField("B", Order = 4, OldOrder = 5, ID = 4, Min = 0f, Max = 1f, Slider = true)]
    public float Blue = 1f;

    /// <summary>The target fog intensity.</summary>
    [InspectorField("Fog Intensity", Order = 5, OldOrder = 6, ID = 5, Min = 0, Max = 20, Slider = true)]
    public int FogIntensity = 3;
}

/// <summary>Enables, disables, or flips referenced objects.</summary>
[ObjectType("Toggle Trigger")]
public sealed partial class ToggleTriggerObjectDefinition : ObjectDefinition
{
    /// <summary>The objects.</summary>
    [InspectorField("Toggle", Order = 2, OldOrder = 3.5f, ID = 2)]
    public ObjectReferences Toggle = new();

    /// <summary>The change to apply.</summary>
    [InspectorField("Mode", Order = 3, OldOrder = 4, ID = 3)]
    public ToggleMode Mode = ToggleMode.On;
}


/// <summary>Defines a coin.</summary>
[ObjectType("Coin")]
public sealed partial class CoinObjectDefinition : UniformScaledObjectDefinition
{
    /// <summary>Whether the coin is rendered.</summary>
    [InspectorField("Visible", Order = 5, OldOrder = 2.5f, ID = 5)]
    public bool Visible = true;
    /// <summary>Whether collecting the coin plays a sound.</summary>
    [InspectorField("Audible", Order = 6, OldOrder = 2.6f, ID = 6)]
    public bool Audible = true;
    /// <summary>Amount of currency coins to award. (kinda pointless, only applies to main levels) </summary>
    [InspectorField("Value", Order = 7, OldOrder = 3.9f, ID = 7)]
    public uint Value = 1;
    /// <summary>The objects triggered when the coin is collected.</summary>
    [InspectorField("On Pickup", Order = 8, OldOrder = 4, ID = 8)]
    public ObjectReferences OnPickup = new();
}

/// <summary>Defines a teleporter that applies an offset to your player (designed for seamless infinite hallways and staircases and the like).</summary>
[ObjectType("Portal")]
public sealed partial class PortalObjectDefinition : ScaledObjectDefinition
{
    /// <summary>Offset to apply to the player.</summary>
    [InspectorField("Offset", Order = 5, OldOrder = 3, ID = 5)]
    public Vector3 Offset = new(0, 10, 0);
}
