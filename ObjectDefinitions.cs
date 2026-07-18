namespace Salmon.Levels;

public abstract partial class ObjectDefinition
{
    [InspectorField("Name", Order = -999)]
    public string Name = "Unknown";

    [InspectorField(label: "ID", Order = -1, ReadOnly = true, NoInspect = true)]
    public ulong ID = Snowflake.CreateULong();

    [InspectorField("Position", Order = 0, Handle = InspectorHandleType.Move)]
    public Vector3 Position = Vector3.zero;

    [InspectorField("Rotation", Order = 1, Handle = InspectorHandleType.Rotate)]
    public Vector3 Rotation = Vector3.zero;
}

public sealed partial class ObjectReferences : ISpecialSerializable
{
    public HashSet<ulong> IDs = new(8);
    public static string GetDisplayPath(ulong id, Group root)
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
    public void SpecialWrite(BinaryWriter writer)
    {
        writer.Write((byte)IDs.Count);
        foreach (var id in IDs)
            writer.Write(id);
    }
    public void SpecialRead(BinaryReader reader)
    {
        byte count = reader.ReadByte();
        IDs.Clear();
        for (var i = 0; i < count; i++)
            IDs.Add(reader.ReadUInt64());
    }
}

public abstract partial class ScaledObjectDefinition : ObjectDefinition
{
    [InspectorField("Scale", Order = 2, Min = 0.05f, Handle = InspectorHandleType.Scale)]
    public Vector3 Scale = Vector3.one;
}

public abstract partial class UniformScaledObjectDefinition : ObjectDefinition
{
    [InspectorField("Scale", Order = 2, Min = 0.05f)]
    public float Scale = 1f;
}


[Serializable]
public enum WallMode : byte
{
    [InspectorName("Visuals & Collision")]
    VisualsCollision,
    Collision,
    Visuals
}

[Serializable]
[ObjectType("Group")]
public sealed partial class Group : UniformScaledObjectDefinition, ISpecialSerializable
{
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
    public List<ObjectDefinition> Objects = [];
    public void SpecialWrite(BinaryWriter writer) => SerializeObjects(writer, Objects);
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

    public static void SerializeObjects(BinaryWriter writer, List<ObjectDefinition> objects)
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

[Serializable]
[ObjectType("Wall")]
public partial class Wall : ScaledObjectDefinition
{
    [InspectorField("Shape", Order = 2.5f, Options = "Shapes")]
    public string Shape = "Box";

    [InspectorField("Material", Order = 3, Options = "Materials")]
    public string Material = "Brick";

    [InspectorField("Damp", Order = 4, Min = 0f, Max = 1f, Slider = true)]
    public float Damp = 1f;

    [InspectorField("Mode", Order = 5)]
    public WallMode Mode = WallMode.VisualsCollision;

    [InspectorField("Deadly", Order = 5.5f)]
    public bool Deadly = false;

    [InspectorField("Break Velocity", Order = 6, Min = -1f, Max = 999f)]
    public float BreakVelocity = -1f;

    [InspectorField("On Break", Order = 7, Old = true)]
    public string TriggerID = "";

    [InspectorField("On Break", Order = 8)]
    public ObjectReferences Trigger = new();
}
[Serializable]
[ObjectType("Light")]
public sealed partial class LightObjectDefinition : ObjectDefinition
{
    [InspectorField("R", Order = 3, Min = 0f, Max = 1f, Slider = true)]
    public float Red = 1f;

    [InspectorField("G", Order = 4, Min = 0f, Max = 1f, Slider = true)]
    public float Green = 1f;

    [InspectorField("B", Order = 5, Min = 0f, Max = 1f, Slider = true)]
    public float Blue = 1f;

    [InspectorField("Intensity", Order = 6, Min = 0f, Max = 999f)]
    public float Intensity = 1f;

    [InspectorField("Radius", Order = 7, Min = 0f, Max = 99f)]
    public float Radius = 10f;
}

[Serializable]
[ObjectType("Magnet Field")]
public sealed partial class MagnetFieldObjectDefinition : ScaledObjectDefinition
{

    [InspectorField("Strength", Order = 3, Min = 0f, Max = 999f)]
    public float Strength = 25f;
}

public enum CardinalDirection : byte
{
    [InspectorName("Z-")]
    ZNeg,
    [InspectorName("Z+")]
    ZPos,
    [InspectorName("X-")]
    XNeg,
    [InspectorName("X+")]
    XPos,
}

public static class CardinalDirectionExtensions
{
    public static float GetYaw(this CardinalDirection direction) => direction switch
    {
        CardinalDirection.ZNeg => -90f,
        CardinalDirection.XPos => 0f,
        CardinalDirection.ZPos => 90f,
        CardinalDirection.XNeg => 180f,
        _ => 0f,
    };
}

[Serializable]
[ObjectType("Checkpoint")]
public sealed partial class CheckpointObjectDefinition : UniformScaledObjectDefinition
{

    [InspectorField("Direction", Order = 3)]
    public CardinalDirection Direction;

    [InspectorField("Visible", Order = 4)]
    public bool Visible = true;
    [InspectorField("Audible", Order = 4.5f)]
    public bool Audible = true;

    [InspectorField("On Hit", Order = 5, Old = true)]
    public string TriggerID = "";

    [InspectorField("On Hit", Order = 6)]
    public ObjectReferences Trigger = new();
}

[Serializable]
[ObjectType("Win")]
public sealed partial class WinObjectDefinition : UniformScaledObjectDefinition
{
    [InspectorField("Visible", Order = 3)]
    public bool Visible = true;
    
    [InspectorField("Visible", Order = 3.5f)]
    public bool Audible = true;


    [InspectorField("On Win", Order = 4, Old = true)]
    public string TriggerID = "";

    [InspectorField("On Win", Order = 4)]
    public ObjectReferences Trigger = new();
}

[Serializable]
[ObjectType("Text")]
public sealed partial class TextObjectDefinition : ScaledObjectDefinition
{

    [InspectorField("Width", Order = 3, Min = 0.1f, Max = 999f)]
    public float Width = 3f;

    [InspectorField("R", Order = 3.1f, Min = 0f, Max = 1f, Slider = true)]
    public float Red = 1f;

    [InspectorField("G", Order = 3.2f, Min = 0f, Max = 1f, Slider = true)]
    public float Green = 1f;

    [InspectorField("B", Order = 3.3f, Min = 0f, Max = 1f, Slider = true)]
    public float Blue = 1f;

    [InspectorField("Font Size", Order = 4, Min = 0.1f, Max = 999f)]
    public float FontSize = 10f;

    [InspectorField("Text", Order = 5, Multiline = true)]
    public string Text = "Text";
}

[Serializable]
[ObjectType("Magnet Point")]
public sealed partial class MagnetPointObjectDefinition : ObjectDefinition
{
    [InspectorField("Strength", Order = 3, Min = -999f, Max = 999f)]
    public float Strength = 25f;

    [InspectorField("Radius", Order = 4, Min = 0f, Max = 999f)]
    public float Radius = 10f;
}


[Serializable]
public enum Operator : byte
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Set
}



[Serializable]
public enum Comparison : byte
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

[Serializable]
public enum ToggleMode : byte
{
    On,
    Off,
    Flip
}

[Serializable]
public enum MoveSpeedMode : byte
{
    [InspectorName("units/second")]
    WorldSpeed,
    [InspectorName("seconds in total")]
    TimeSeconds
}
[Serializable]
public enum RotationSpeedMode : byte
{
    [InspectorName("degrees/second")]
    WorldSpeed,
    [InspectorName("seconds in total")]
    TimeSeconds
}

public enum EasingType
{
    [InspectorName("linear")]
    Linear,

    [InspectorName("quadratic")]
    Quadratic,

    [InspectorName("cubic")]
    Cubic,

    [InspectorName("sine")]
    Sine,

    [InspectorName("circular")]
    Circ,

    [InspectorName("exponential")]
    Expo,

    [InspectorName("back")]
    Back,

    [InspectorName("bounce")]
    Bounce,

    [InspectorName("elastic")]
    Elastic
}

public enum EasingDirection
{
    [InspectorName("in")]
    In,

    [InspectorName("out")]
    Out,

    [InspectorName("in out")]
    InOut
}

[Serializable]
[ObjectType("Move Trigger")]
public sealed partial class MoveTriggerObjectDefinition : ObjectDefinition
{
    [InspectorField("Move", Order = 3, Old = true)]
    public string MoveID = "";

    [InspectorField("Move", Order = 3.5f)]
    public ObjectReferences Move = new();

    [InspectorField("Move Speed Mode", Order = 4)]
    public MoveSpeedMode MoveSpeedMode = MoveSpeedMode.WorldSpeed;

    [InspectorField("Move Speed", Order = 5, Min = 0f, Max = 999f)]
    public float MoveSpeed = 1f;

    [InspectorField("Easing Type", Order = 5.5f)]
    public EasingType EasingType = EasingType.Linear;

    [InspectorField("Easing Direction", Order = 5.6f)]
    public EasingDirection EasingDirection = EasingDirection.In;

    [InspectorField("On Done", Order = 6, Old = true)]
    public string TriggerID = "";

    [InspectorField("On Done", Order = 7)]
    public ObjectReferences Trigger = new();
}

[Serializable]
[ObjectType("Rotation Trigger")]
public sealed partial class RotationTriggerObjectDefinition : ObjectDefinition
{
    [InspectorField("Rotate", Order = 3, Old = true)]
    public string RotateID = "";
    [InspectorField("Rotate", Order = 3.5f)]
    public ObjectReferences Rotate = new();

    [InspectorField("Rotation Speed Mode", Order = 4)]
    public RotationSpeedMode RotationSpeedMode = RotationSpeedMode.WorldSpeed;

    [InspectorField("Rotation Speed", Order = 5, Min = 0f, Max = 999f)]
    public float RotationSpeed = 90f;

    [InspectorField("Easing Type", Order = 5.5f)]
    public EasingType EasingType = EasingType.Linear;

    [InspectorField("Easing Direction", Order = 5.6f)]
    public EasingDirection EasingDirection = EasingDirection.InOut;

    [InspectorField("On Done", Order = 6, Old = true)]
    public string TriggerID = "";

    [InspectorField("On Done", Order = 7)]
    public ObjectReferences Trigger = new();
}

[Serializable]
[ObjectType("Teleport Trigger")]
public sealed partial class TeleportTriggerObjectDefinition : ObjectDefinition
{
    [InspectorField("On Done", Order = 3)]
    public ObjectReferences Trigger = new();
}

[Serializable]
[ObjectType("Death Trigger")]
public sealed partial class DeathTriggerObjectDefinition : ObjectDefinition
{
    [InspectorField("On Respawn", Order = 3)]
    public ObjectReferences OnRespawn = new();
}

[Serializable]
[ObjectType("Follow Trigger")]
public sealed partial class FollowTriggerObjectDefinition : ObjectDefinition
{
    [InspectorField("Follow Target", Order = 3)]
    public FollowTarget FollowTarget = FollowTarget.Player;

    [InspectorField("Follow Rate", Order = 4, Min = 0f, Max = 1000f)]
    public float FollowRate = 3f;

    [InspectorField("Snap", Order = 4.5f)]
    public bool Snap = false;

    [InspectorField("Follow", Order = 5)]
    public ObjectReferences Follow = new();

    [InspectorField("Run On Start", Order = 6)]
    public bool RunOnStart = false;
}

public enum FollowTarget : byte
{
    Camera,
    Player,
}

[Serializable]
[ObjectType("Destroy Trigger")]
public sealed partial class DestroyTriggerObjectDefinition : ObjectDefinition
{
    [InspectorField("Destroy", Order = 3, Old = true)]
    public string DestroyID = "";

    [InspectorField("Destroy", Order = 3.5f)]
    public ObjectReferences Destroy = new();

    [InspectorField("Wait", Order = 6, Min = 0f, Max = 999f)]
    public float Wait = 0f;
}

[Serializable]
[ObjectType("Touch Trigger")]
public sealed partial class TouchTriggerObjectDefinition : UniformScaledObjectDefinition
{

    [InspectorField("Trigger", Order = 3, Old = true)]
    public string TriggerID = "";

    [InspectorField("Trigger", Order = 3.5f)]
    public ObjectReferences Trigger = new();

    [InspectorField("Cooldown", Order = 4, Min = -1f, Max = 999f)]
    public float Cooldown = 0f;
}

[Serializable]
[ObjectType("Delay Trigger")]
public sealed partial class DelayTriggerObjectDefinition : ObjectDefinition
{
    [InspectorField("Trigger", Order = 3, Old = true)]
    public string TriggerID = "";

    [InspectorField("Trigger", Order = 3.5f)]
    public ObjectReferences Trigger = new();

    [InspectorField("Wait", Order = 4, Min = 0f, Max = 999f)]
    public float Wait = 0f;

    [InspectorField("Run On Start", Order = 5)]
    public bool RunOnStart = false;
}

[Serializable]
[ObjectType("Counter Trigger")]
public sealed partial class CounterTriggerObjectDefinition : ObjectDefinition
{
    [InspectorField("Counter", Order = 3)]
    public string Counter = "";
    [InspectorField("Operator", Order = 5)]
    public Operator Operator = Operator.Set;
    [InspectorField("Operand", Order = 6)]
    public string Operand = "1";
}

[Serializable]
[ObjectType("If Trigger")]
public sealed partial class IfTriggerObjectDefinition : ObjectDefinition
{
    [InspectorField("Counter", Order = 3)]
    public string Counter = "";

    [InspectorField("Operand", Order = 4)]
    public int Operand = 1;

    [InspectorField("Comparison", Order = 5)]
    public Comparison Operator = Comparison.Equal;

    [InspectorField("True Trigger", Order = 6, Old = true)]
    public string TrueTriggerID = "";

    [InspectorField("False Trigger", Order = 7, Old = true)]
    public string FalseTriggerID = "";

    [InspectorField("True Trigger", Order = 8)]
    public ObjectReferences TrueTrigger = new();

    [InspectorField("False Trigger", Order = 9)]
    public ObjectReferences FalseTrigger = new();
}

[Serializable]
[ObjectType("Set Material Trigger")]
public sealed partial class SetMaterialTriggerObjectDefinition : ObjectDefinition
{
    [InspectorField("Material", Order = 3, Options = "Materials")]
    public string Material = "Brick";

    [InspectorField("Wall", Order = 4, Old = true)]
    public string WallID = "";

    [InspectorField("Wall", Order = 5)]
    public ObjectReferences Wall = new();
}

[Serializable]
[ObjectType("Sky Colour Trigger")]
public sealed partial class SkyColourTriggerObjectDefinition : ObjectDefinition
{
    [InspectorField("R", Order = 3, Min = 0f, Max = 1f, Slider = true)]
    public float Red = 1f;

    [InspectorField("G", Order = 4, Min = 0f, Max = 1f, Slider = true)]
    public float Green = 1f;

    [InspectorField("B", Order = 5, Min = 0f, Max = 1f, Slider = true)]
    public float Blue = 1f;

    [InspectorField("Fog Intensity", Order = 6, Min = 0, Max = 20, Slider = true)]
    public int FogIntensity = 3;
}

[Serializable]
[ObjectType("Toggle Trigger")]
public sealed partial class ToggleTriggerObjectDefinition : ObjectDefinition
{
    [InspectorField("Toggle", Order = 3, Old = true)]
    public string ToggleID = "";

    [InspectorField("Toggle", Order = 3.5f)]
    public ObjectReferences Toggle = new();

    [InspectorField("Mode", Order = 4)]
    public ToggleMode Mode = ToggleMode.On;
}


[Serializable]
[ObjectType("Coin")]
public sealed partial class CoinObjectDefinition : UniformScaledObjectDefinition
{
    [InspectorField("Visible", Order = 2.5f)]
    public bool Visible = true;
    [InspectorField("Audible", Order = 2.6f)]
    public bool Audible = true;

    [InspectorField("On Pickup", Order = 3, Old = true)]
    public string OnPickupID = "";

    [InspectorField("On Pickup", Order = 4)]
    public ObjectReferences OnPickup = new();
}
