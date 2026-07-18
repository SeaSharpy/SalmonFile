using System.Reflection;

namespace Salmon.Levels;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ObjectTypeAttribute : Attribute
{
    public string DisplayName { get; }

    public ObjectTypeAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}

public static class ObjectMetadata
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
        ["Touch Trigger"] = 7,
        ["Move Trigger"] = 8,
        ["Rotation Trigger"] = 9,
        ["Toggle Trigger"] = 10,
        ["Destroy Trigger"] = 11,
        ["Teleport Trigger"] = 12,
        ["Follow Trigger"] = 13,
        ["Set Material Trigger"] = 14,
        ["Sky Colour Trigger"] = 15,
        ["Death Trigger"] = 16,
        ["Delay Trigger"] = 17,
        ["Counter Trigger"] = 18,
        ["If Trigger"] = 19,
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
