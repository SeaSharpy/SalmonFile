using System.Reflection;
using System.Collections;
using System.Text;
using System.Runtime.CompilerServices;
namespace Salmon;
/// <summary>Provides custom payload serialization for types that require data beyond simple fields.</summary>
public interface ISpecialSerializable
{
    /// <summary>Writes the custom payload after the object's regular fields.</summary>
    /// <param name="writer">The destination writer.</param>
    void SpecialWrite(BinaryWriter writer);
    /// <summary>Reads the custom payload after the object's regular fields.</summary>
    /// <param name="reader">The source reader.</param>
    void SpecialRead(BinaryReader reader);
}
/// <summary>Serializes inspector fields into the version-tolerant binary format used by Salmon files.</summary>
public static class DynamicSerializer
{
    private const int MaximumCollectionCount = ushort.MaxValue;
    [ThreadStatic]
    internal static bool OldRead;
    internal static object DeserializeOld(Type type, byte[] bytes)
    {
        var oldRead = OldRead;
        OldRead = true;
        try
        {
            return Deserialize(type, bytes);
        }
        finally
        {
            OldRead = oldRead;
        }
    }
    /// <summary>Serializes an object to a new byte array.</summary>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>The serialized object payload.</returns>
    public static byte[] Serialize(object obj)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        Serialize(obj, writer);
        return stream.ToArray();
    }
    /// <summary>Serializes an object to a binary writer.</summary>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="writer">The destination writer.</param>
    public static void Serialize(object obj, BinaryWriter writer)
    {
        var type = obj.GetType();
        if (GeneratedDynamicSerializers.TryWrite(obj, writer))
            return;
        var cached = TypeCache.Get(type);
        if (cached.Unmanaged != null)
        {
            cached.Unmanaged.Write(writer, cached.Unmanaged.Wrap(obj));
            return;
        }
        var fields = cached.Fields.Where(field => !field.Attribute.NoSave && !field.Attribute.Old);
        writer.Write((uint)fields.Count());
        foreach (var field in fields)
        {
            writer.Write(field.Attribute.ID);
            WriteSection(writer, () => WriteValue(writer, field.ValueType, field.GetValue(obj)));
        }
        writer.Write(obj is ISpecialSerializable);
        if (obj is ISpecialSerializable special)
            WriteSection(writer, () => special.SpecialWrite(writer));
    }
    private static void WriteSection(BinaryWriter writer, Action write)
    {
        var stream = writer.BaseStream;
        var position = stream.Position;
        writer.Write(0);
        write();
        var end = stream.Position;
        stream.Position = position;
        writer.Write((uint)(end - position - sizeof(uint)));
        stream.Position = end;
    }

    /// <summary>Deserializes an object from a byte array.</summary>
    /// <param name="type">The concrete object type to create.</param>
    /// <param name="bytes">The serialized object payload.</param>
    /// <returns>The deserialized object.</returns>
    public static object Deserialize(Type type, byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var reader = new BinaryReader(stream);
        return Deserialize(type, reader);
    }

    /// <summary>Deserializes and creates an object from a binary reader.</summary>
    /// <param name="type">The concrete object type to create.</param>
    /// <param name="reader">The source reader.</param>
    /// <returns>The deserialized object.</returns>
    public static object Deserialize(Type type, BinaryReader reader)
    {
        if (GeneratedDynamicSerializers.TryRead(type, reader, out var generated))
            return generated;
        var raw = TypeCache.Get(type).Unmanaged;
        if (raw != null)
            return raw.Read(reader, 1).GetValue(0);
        var obj = Activator.CreateInstance(type);
        Deserialize(type, obj, reader);
        return obj;
    }
    /// <summary>Populates an existing object from a binary reader.</summary>
    /// <param name="type">The object's concrete type.</param>
    /// <param name="obj">The object to populate.</param>
    /// <param name="reader">The source reader.</param>
    public static void Deserialize(Type type, object obj, BinaryReader reader)
    {
        var serializerType = TypeCache.Get(type); 
        var count = checked((ushort)reader.ReadUInt32());
        for (var i = 0; i < count; i++)
        {
            DynamicInspectorField field;
            var key = OldRead ? reader.ReadSingle() : reader.ReadByte();
            var found = OldRead
                ? serializerType.FieldsByOldOrder.TryGetValue(key, out field)
                : serializerType.FieldsByID.TryGetValue((byte)key, out field);
            var length = reader.ReadUInt32();
            var startPosition = reader.BaseStream.Position;
            if (!found)
            {
                reader.BaseStream.Position += length;
                continue;
            }
            using var sectionStream = new LimitedReadStream(reader.BaseStream, length);
            using var sectionReader = new BinaryReader(sectionStream, Encoding.UTF8, true);
            object value;
            try
            {
                value = ReadValue(sectionReader, field.ValueType);
            }
            catch (EndOfStreamException exception)
            {
                Debug.LogWarning($"Skipping field {key} because it read past length {length}: {exception.Message}");
                reader.BaseStream.Position = startPosition + length;
                continue;
            }
            if (sectionStream.Position != length)
            {
                Debug.LogWarning($"Read has length {length} but was read as {sectionStream.Position}.");
                sectionStream.Position = length;
            }
            reader.BaseStream.Position = startPosition + length;
            field.SetValue(obj, value);
        }
        if (reader.ReadBoolean())
        {
            var length = reader.ReadUInt32();
            var startPosition = reader.BaseStream.Position;
            if (obj is ISpecialSerializable specialSerializable)
            {
                using var sectionStream = new LimitedReadStream(reader.BaseStream, length);
                using var sectionReader = new BinaryReader(sectionStream, Encoding.UTF8, true);
                try
                {
                    specialSerializable.SpecialRead(sectionReader);
                }
                catch (EndOfStreamException exception)
                {
                    Debug.LogWarning($"Skipping special read because it read past length {length}: {exception.Message}");
                }
                if (sectionStream.Position != length)
                {
                    Debug.LogWarning($"Special read has length {length} but was read as {sectionStream.Position}.");
                }
            }
            reader.BaseStream.Position = startPosition + length;
        }
    }
    internal static void WriteValue(BinaryWriter writer, Type type, object value)
    {
        if (type.IsEnum)
        {
            type = Enum.GetUnderlyingType(type);
            value = Convert.ChangeType(value, type);
        }
        if (SimpleSerializers.TryGetValue(type, out var serializer))
            serializer.write(writer, value);
        else if (type == typeof(string))
            writer.Write((string)value);
        else if (type.IsArray)
            WriteArray(writer, type, (Array)value);
        else if (TryGetDictionaryTypes(type, out var keyType, out var valueType))
            WriteDictionary(writer, keyType, valueType, value);
        else if (TryGetCollectionElementType(type, out var elementType))
            WriteCollection(writer, elementType, value);
        else
            Serialize(value, writer);
    }
    internal static object ReadValue(BinaryReader reader, Type type)
    {
        if (type.IsEnum)
            return Enum.ToObject(type, SimpleSerializers[Enum.GetUnderlyingType(type)].read(reader));
        if (SimpleSerializers.TryGetValue(type, out var serializer))
            return serializer.read(reader);
        if (type == typeof(string))
            return reader.ReadString();
        if (type.IsArray)
            return ReadArray(reader, type);
        if (TryGetDictionaryTypes(type, out var keyType, out var valueType))
            return ReadDictionary(reader, type, keyType, valueType);
        if (TryGetCollectionElementType(type, out var elementType))
            return ReadCollection(reader, type, elementType);
        return Deserialize(type, reader);
    }
    private static void WriteArray(BinaryWriter writer, Type type, Array values)
    {
        if (type.GetArrayRank() != 1)
            throw new NotSupportedException("Only one-dimensional arrays are supported.");

        if (values == null)
        {
            writer.Write(-1);
            return;
        }

        writer.Write(values.Length);
        if (type == typeof(byte[]))
        {
            writer.Write((byte[])values);
            return;
        }
        var elementType = type.GetElementType()!;
        var raw = TypeCache.Get(elementType).Unmanaged;
        if (raw != null)
        {
            raw.Write(writer, values);
            return;
        }
        foreach (var value in values)
            WriteValue(writer, elementType, value);
    }
    private static Array ReadArray(BinaryReader reader, Type type)
    {
        if (type.GetArrayRank() != 1)
            throw new NotSupportedException("Only one-dimensional arrays are supported.");

        var length = reader.ReadInt32();
        if (length == -1)
            return null;
        if (length < 0 || length > reader.BaseStream.Length - reader.BaseStream.Position)
            throw new InvalidDataException("Array length exceeds the available data or is negative.");
        if (type == typeof(byte[]))
            return reader.ReadBytes(length);

        var elementType = type.GetElementType()!;
        var raw = TypeCache.Get(elementType).Unmanaged;
        if (raw != null)
            return raw.Read(reader, length);
        var values = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
        for (var i = 0; i < length; i++)
            values.Add(ReadValue(reader, elementType));
        var array = Array.CreateInstance(elementType, length);
        values.CopyTo(array, 0);
        return array;
    }

    private static void WriteDictionary(BinaryWriter writer, Type keyType, Type valueType, object dictionary)
    {
        if (dictionary == null)
        {
            writer.Write(-1);
            return;
        }

        var values = dictionary as IEnumerable;
        var count = ValidateCollectionCount(GetCollectionCount(dictionary));
        writer.Write(count);
        foreach (var value in values)
        {
            var entryType = value.GetType();
            WriteValue(writer, keyType, entryType.GetProperty("Key").GetValue(value));
            WriteValue(writer, valueType, entryType.GetProperty("Value").GetValue(value));
        }
    }
    private static object ReadDictionary(BinaryReader reader, Type type, Type keyType, Type valueType)
    {
        var count = ReadCollectionCount(reader);
        if (count < 0)
            return null;

        var dictionary = Activator.CreateInstance(type.IsInterface || type.IsAbstract ? typeof(Dictionary<,>).MakeGenericType(keyType, valueType) : type);
        for (var i = 0; i < count; i++)
            AddValue(dictionary, DictionaryAddMethods, "Dictionary", ReadValue(reader, keyType), ReadValue(reader, valueType));
        return dictionary;
    }
    private static void WriteCollection(BinaryWriter writer, Type elementType, object collection)
    {
        if (collection == null)
        {
            writer.Write(-1);
            return;
        }

        writer.Write(ValidateCollectionCount(GetCollectionCount(collection)));
        foreach (var value in (IEnumerable)collection)
            WriteValue(writer, elementType, value);
    }
    private static object ReadCollection(BinaryReader reader, Type type, Type elementType)
    {
        var count = ReadCollectionCount(reader);
        if (count < 0)
            return null;

        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (IList)Activator.CreateInstance(listType);
        for (var i = 0; i < count; i++)
            list.Add(ReadValue(reader, elementType));
        return CreateCollection(type, elementType, list);
    }
    private static int GetCollectionCount(object collection)
    {
        if (collection is ICollection nonGenericCollection)
            return nonGenericCollection.Count;

        var countProperty = collection.GetType().GetProperty("Count");
        if (countProperty != null && countProperty.PropertyType == typeof(int))
            return (int)countProperty.GetValue(collection);

        return ((IEnumerable)collection).Cast<object>().Count();
    }
    private static int ReadCollectionCount(BinaryReader reader)
    {
        var count = reader.ReadInt32();
        if (count == -1)
            return count;
        if (count < 0)
            throw new InvalidDataException("Collection count cannot be negative.");
        return checked((ushort)count);
    }
    private static int ValidateCollectionCount(int count)
    {
        if (count > MaximumCollectionCount)
            throw new InvalidDataException($"Collections cannot contain more than {MaximumCollectionCount} items.");
        return count;
    }
    private static readonly string[] DictionaryAddMethods = ["Add", "TryAdd"];
    private static readonly string[] CollectionAddMethods = ["Add", "Enqueue", "Push", "TryAdd"];
    private static object CreateCollection(Type type, Type elementType, IList values)
    {
        if (type.IsInterface || type.IsAbstract)
        {
            if (IsGenericInterface(type, typeof(ISet<>)))
                return CreateFromEnumerable(typeof(HashSet<>).MakeGenericType(elementType), elementType, values);
            return values;
        }

        var collection = CreateFromEnumerable(type, elementType, values);
        if (collection != null)
            return collection;

        collection = Activator.CreateInstance(type);
        foreach (var value in values)
            AddValue(collection, CollectionAddMethods, "Collection", value);
        return collection;
    }
    private static object CreateFromEnumerable(Type type, Type elementType, IList values)
    {
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
        var constructor = type.GetConstructor([enumerableType]);
        if (constructor == null)
            return null;
        return constructor.Invoke([values]);
    }
    private static void AddValue(object target, string[] methods, string kind, params object[] values)
    {
        var type = target.GetType();
        foreach (var name in methods)
        {
            var method = GetInstanceMethod(type, name, values);
            if (method == null)
                continue;
            method.Invoke(target, values);
            return;
        }
        throw new NotSupportedException($"{kind} type {type.Name} does not support adding values.");
    }
    private static bool TryGetDictionaryTypes(Type type, out Type keyType, out Type valueType)
    {
        keyType = null;
        valueType = null;
        var dictionaryType = GetGenericType(type, typeof(IDictionary<,>)) ?? GetGenericType(type, typeof(IReadOnlyDictionary<,>));
        if (dictionaryType == null)
            return false;
        var arguments = dictionaryType.GetGenericArguments();
        keyType = arguments[0];
        valueType = arguments[1];
        return true;
    }
    private static bool TryGetCollectionElementType(Type type, out Type elementType)
    {
        elementType = null;
        if (type == typeof(string))
            return false;
        var enumerableType = GetGenericType(type, typeof(IEnumerable<>));
        if (enumerableType == null)
            return false;
        elementType = enumerableType.GetGenericArguments()[0];
        return true;
    }
    private static Type GetGenericType(Type type, Type genericType)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType)
            return type;
        return type.GetInterfaces().FirstOrDefault(interfaceType => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == genericType);
    }
    private static bool IsGenericInterface(Type type, Type genericType) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == genericType;
    private static MethodInfo GetInstanceMethod(Type type, string name, params object[] arguments) =>
        type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(method => MethodMatches(method, name, arguments));
    private static bool MethodMatches(MethodInfo method, string name, object[] arguments)
    {
        if (method.Name != name)
            return false;
        var parameters = method.GetParameters();
        if (parameters.Length != arguments.Length)
            return false;
        for (var i = 0; i < parameters.Length; i++)
            if (arguments[i] != null && !parameters[i].ParameterType.IsAssignableFrom(arguments[i].GetType()))
                return false;
        return true;
    }
    private static readonly Dictionary<Type, (Action<BinaryWriter, object> write, Func<BinaryReader, object> read)> SimpleSerializers = new()
    {
        [typeof(bool)] = ((writer, value) => writer.Write((bool)value), reader => reader.ReadBoolean()),
        [typeof(byte)] = ((writer, value) => writer.Write((byte)value), reader => reader.ReadByte()),
        [typeof(sbyte)] = ((writer, value) => writer.Write((sbyte)value), reader => reader.ReadSByte()),
        [typeof(short)] = ((writer, value) => writer.Write((short)value), reader => reader.ReadInt16()),
        [typeof(ushort)] = ((writer, value) => writer.Write((ushort)value), reader => reader.ReadUInt16()),
        [typeof(int)] = ((writer, value) => writer.Write((int)value), reader => reader.ReadInt32()),
        [typeof(uint)] = ((writer, value) => writer.Write((uint)value), reader => reader.ReadUInt32()),
        [typeof(long)] = ((writer, value) => writer.Write((long)value), reader => reader.ReadInt64()),
        [typeof(ulong)] = ((writer, value) => writer.Write((ulong)value), reader => reader.ReadUInt64()),
        [typeof(float)] = ((writer, value) => writer.Write((float)value), reader => reader.ReadSingle()),
        [typeof(double)] = ((writer, value) => writer.Write((double)value), reader => reader.ReadDouble()),
        [typeof(decimal)] = ((writer, value) => writer.Write((decimal)value), reader => reader.ReadDecimal()),
        [typeof(char)] = ((writer, value) => writer.Write((char)value), reader => reader.ReadChar()),
        [typeof(DateTime)] = ((writer, value) => writer.Write(((DateTime)value).ToBinary()), reader => DateTime.FromBinary(reader.ReadInt64())),
        [typeof(TimeSpan)] = ((writer, value) => writer.Write(((TimeSpan)value).Ticks), reader => TimeSpan.FromTicks(reader.ReadInt64())),
        [typeof(Vector3)] = ((writer, value) => writer.Write((Vector3)value), reader => reader.ReadVector3()),
        [typeof(Quaternion)] = ((writer, value) => writer.Write((Quaternion)value), reader => reader.ReadQuaternion()),
    };
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write(this BinaryWriter writer, Vector3 vector)
    {
        writer.Write(vector.x);
        writer.Write(vector.y);
        writer.Write(vector.z);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    internal static Vector3 ReadVector3(this BinaryReader reader) =>
        new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    internal static Quaternion ReadQuaternion(this BinaryReader reader) =>
        new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    internal static void Write(this BinaryWriter writer, Quaternion quaternion)
    {
        writer.Write(quaternion.x);
        writer.Write(quaternion.y);
        writer.Write(quaternion.z);
        writer.Write(quaternion.w);
    }
}
