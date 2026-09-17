#if !DEMO_APP
using System.Reflection;

namespace Salmon.Levels.Objects;

/// <summary>Associates a serialized level object type with its stable display name.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ObjectTypeAttribute : Attribute
{
    /// <summary>The serialized display name.</summary>
    public string DisplayName { get; }

    /// <summary>Creates object type metadata.</summary>
    /// <param name="displayName">The stable name written to Salmon files.</param>
    public ObjectTypeAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}

internal static class ObjectMetadata
{
    private static readonly IReadOnlyDictionary<string, int> ObjectTypePriorities = new Dictionary<string, int>
    {
        ["Wall"] = 0,
        ["Checkpoint"] = 1,
        ["Group"] = 2,
        ["Win"] = 3,
        ["Coin"] = 4,
        ["Light"] = 5,
        ["Text"] = 6,
        ["Magnet Field"] = 7,
        ["Magnet Point"] = 8,
        ["Touch Trigger"] = 9,
        ["Move Trigger"] = 10,
        ["Rotation Trigger"] = 11,
        ["Toggle Trigger"] = 12,
        ["Destroy Trigger"] = 13,
        ["Teleport Trigger"] = 14,
        ["Portal"] = 15,
        ["Follow Trigger"] = 16,
        ["Set Material Trigger"] = 17,
        ["Sky Colour Trigger"] = 18,
        ["Death Trigger"] = 19,
        ["Delay Trigger"] = 20,
        ["Counter Trigger"] = 21,
        ["If Trigger"] = 22,
        ["Random Trigger"] = 23,
        ["Builtin"] = 24,
    };
    private static readonly Dictionary<Type, string> DisplayNamesByType = [];
    private static readonly Dictionary<Type, byte[]> DisplayBytesByType = [];
    private static readonly Dictionary<string, Type> TypesByDisplayName = [];

    public static readonly IReadOnlyList<Type> KnownObjectTypes;

    static ObjectMetadata()
    {
        var knownObjectTypes = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(type =>
                !type.IsAbstract &&
                typeof(ObjectDefinition).IsAssignableFrom(type))
            .Select(type => new
            {
                Type = type,
                Attribute = type.GetCustomAttribute<ObjectTypeAttribute>()
            })
            .Where(entry => entry.Attribute != null)
            .OrderBy(entry => ObjectTypePriorities.GetValueOrDefault(entry.Attribute.DisplayName, int.MaxValue))
            .ThenBy(entry => entry.Attribute.DisplayName)
            .ToArray();

        var types = new Type[knownObjectTypes.Length];

        for (var i = 0; i < knownObjectTypes.Length; i++)
        {
            var type = knownObjectTypes[i].Type;
            var displayName = knownObjectTypes[i].Attribute.DisplayName;
            TypesByDisplayName[displayName] = type;
            DisplayNamesByType[type] = displayName;
            DisplayBytesByType[type] = EncodeDisplayName(displayName);
            types[i] = type;
        }

        KnownObjectTypes = types;
    }

    public static string GetDisplayName(Type type) => DisplayNamesByType[type];

    public static string GetDisplayName(
        this ObjectDefinition levelObject) => GetDisplayName(levelObject.GetType());

    public static byte[] GetDisplayBytes(Type type) => DisplayBytesByType[type];

    public static byte[] GetDisplayBytes(
        this ObjectDefinition levelObject) => GetDisplayBytes(levelObject.GetType());

    public static Type FromDisplayName(string displayName) => TypesByDisplayName.GetValueOrDefault(displayName);

    private static byte[] EncodeDisplayName(string displayName)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(
            stream,
            Encoding.UTF8,
            true
        );

        writer.Write(displayName);
        writer.Flush();

        return stream.ToArray();
    }
}
#endif
