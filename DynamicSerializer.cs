using System.Reflection;
using System.Collections;
using System.Text;
namespace Salmon;
public interface ISpecialSerializable
{
    void SpecialWrite(BinaryWriter writer);
    void SpecialRead(BinaryReader reader);
}
public static class DynamicSerializer
{
    private const int MaximumCollectionCount = ushort.MaxValue;
    public static byte[] Serialize(object obj)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        Serialize(obj, writer);
        var bytes = stream.ToArray();
        
        return bytes;
    }
    public static void Serialize(object obj, BinaryWriter writer)
    {
        // Get type from cache
        var type = obj.GetType();
        if (GeneratedDynamicSerializers.TryWrite(obj, writer))
            return;
        var serializerType = TypeCache.Get(type);

        // Store stream and position for field count
        var stream = writer.BaseStream;
        var countPosition = stream.Position;
        writer.Write(0);
        var fieldCount = 0;

        foreach (DynamicInspectorField field in serializerType.Fields)
        {
            if (field.Attribute.NoSave || field.Attribute.Old)
                continue;
            // Get the field value and increment field count
            var value = field.GetValue(obj);
            fieldCount++;

            // Write the order
            writer.Write(field.Order);

            // Store the position for length
            var lengthPosition = stream.Position;
            writer.Write(0);

            // Write the value and have it's length
            var valueStart = stream.Position;
            WriteValue(writer, field, value);
            var valueEnd = stream.Position;

            var length = valueEnd - valueStart;

            // Go back and write the length, then go forward again for the next value
            stream.Position = lengthPosition;
            writer.Write((uint)length);
            stream.Position = valueEnd;
        }
        // Go back and write the count, and then go forward again
        var endPosition = stream.Position;
        stream.Position = countPosition;
        writer.Write((uint)fieldCount);
        stream.Position = endPosition;

        if (obj is ISpecialSerializable specialSerializable)
        {
            // Write special serializable with length position stored
            writer.Write(true);
            var lengthPosition = stream.Position;
            writer.Write(0);

            // Write special serializable and have its length
            var specialStart = stream.Position;
            specialSerializable.SpecialWrite(writer);
            var specialEnd = stream.Position;

            var length = specialEnd - specialStart;

            // Go back and write the length, then go forward again
            stream.Position = lengthPosition;
            writer.Write(value: (uint)length);
            stream.Position = specialEnd;
        }
        else
            writer.Write(false);
    }

    public static object Deserialize(Type type, byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var reader = new BinaryReader(stream);
        return Deserialize(type, reader);
    }

    public static object Deserialize(Type type, BinaryReader reader)
    {
        if (GeneratedDynamicSerializers.TryRead(type, reader, out var obj))
            return obj;
        obj = Activator.CreateInstance(type);
        Deserialize(type, obj, reader);
        return obj;
    }
    public static void Deserialize(Type type, object obj, BinaryReader reader)
    {
        var serializerType = TypeCache.Get(type); 
        var count = checked((ushort)reader.ReadUInt32());
        for (var i = 0; i < count; i++)
        {
            var order = reader.ReadSingle();
            var length = reader.ReadUInt32();
            var startPosition = reader.BaseStream.Position;
            if (!serializerType.FieldsByOrder.TryGetValue(order, out var field))
            {
                reader.BaseStream.Position += length;
                continue;
            }
            using var sectionStream = new LimitedReadStream(reader.BaseStream, length);
            using var sectionReader = new BinaryReader(sectionStream, Encoding.UTF8, true);
            object value;
            try
            {
                value = ReadValue(sectionReader, field);
            }
            catch (EndOfStreamException exception)
            {
                Debug.LogWarning($"Skipping field {order} because it read past length {length}: {exception.Message}");
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
    private static void WriteValue(BinaryWriter writer, DynamicInspectorField field, object value) => WriteValue(writer, field.ValueType, value);
    private static void WriteValue(BinaryWriter writer, Type type, object value)
    {
        if (type == typeof(string))
            writer.Write((string)value);
        else if (type.IsArray)
            WriteArray(writer, type, (Array)value);
        else if (TryGetDictionaryTypes(type, out var keyType, out var valueType))
            WriteDictionary(writer, keyType, valueType, value);
        else if (TryGetCollectionElementType(type, out var elementType))
            WriteCollection(writer, elementType, value);
        else if (type.IsEnum)
        {
            var underlyingType = Enum.GetUnderlyingType(type);
            WriteSimpleValue(writer, underlyingType, Convert.ChangeType(value, underlyingType));
        }
        else if (WriteSimpleValue(writer, type, value))
        {

        }
        else
            Serialize(value, writer);
    }
    private static object ReadValue(BinaryReader reader, DynamicInspectorField field) => ReadValue(reader, field.ValueType);
    private static object ReadValue(BinaryReader reader, Type type)
    {
        if (type == typeof(string))
            return reader.ReadString();
        if (type.IsArray)
            return ReadArray(reader, type);
        if (TryGetDictionaryTypes(type, out var keyType, out var valueType))
            return ReadDictionary(reader, type, keyType, valueType);
        if (TryGetCollectionElementType(type, out var elementType))
            return ReadCollection(reader, type, elementType);
        if (type.IsEnum && ReadSimpleValue(reader, Enum.GetUnderlyingType(type), out var value))
            return Enum.ToObject(type, value);
        if (ReadSimpleValue(reader, type, out var simpleValue))
            return simpleValue;
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

        writer.Write(ValidateCollectionCount(values.Length));
        var elementType = type.GetElementType()!;
        foreach (var value in values)
            WriteValue(writer, elementType, value);
    }
    private static Array ReadArray(BinaryReader reader, Type type)
    {
        if (type.GetArrayRank() != 1)
            throw new NotSupportedException("Only one-dimensional arrays are supported.");

        var length = ReadCollectionCount(reader);
        if (length < 0)
            return null;
        var elementType = type.GetElementType()!;
        var values = Array.CreateInstance(elementType, length);
        for (var i = 0; i < length; i++)
            values.SetValue(ReadValue(reader, elementType), i);
        return values;
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

        var dictionary = CreateDictionary(type, keyType, valueType);
        for (var i = 0; i < count; i++)
        {
            var key = ReadValue(reader, keyType);
            var value = ReadValue(reader, valueType);
            AddDictionaryValue(dictionary, key, value);
        }
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

        var count = 0;
        foreach (var value in (IEnumerable)collection)
            count++;
        return count;
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
    private static object CreateDictionary(Type type, Type keyType, Type valueType)
    {
        if (type.IsInterface || type.IsAbstract)
            return Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(keyType, valueType));
        return Activator.CreateInstance(type);
    }
    private static void AddDictionaryValue(object dictionary, object key, object value)
    {
        var add = GetInstanceMethod(dictionary.GetType(), "Add", key, value);
        if (add != null)
        {
            add.Invoke(dictionary, [key, value]);
            return;
        }

        var tryAdd = GetInstanceMethod(dictionary.GetType(), "TryAdd", key, value);
        if (tryAdd != null)
        {
            tryAdd.Invoke(dictionary, [key, value]);
            return;
        }

        throw new NotSupportedException($"Dictionary type {dictionary.GetType().Name} does not support adding values.");
    }
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
            AddCollectionValue(collection, value);
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
    private static void AddCollectionValue(object collection, object value)
    {
        var type = collection.GetType();
        var add = GetInstanceMethod(type, "Add", value);
        if (add != null)
        {
            add.Invoke(collection, [value]);
            return;
        }

        var enqueue = GetInstanceMethod(type, "Enqueue", value);
        if (enqueue != null)
        {
            enqueue.Invoke(collection, [value]);
            return;
        }

        var push = GetInstanceMethod(type, "Push", value);
        if (push != null)
        {
            push.Invoke(collection, [value]);
            return;
        }

        var tryAdd = GetInstanceMethod(type, "TryAdd", value);
        if (tryAdd != null)
        {
            tryAdd.Invoke(collection, [value]);
            return;
        }

        throw new NotSupportedException($"Collection type {type.Name} does not support adding values.");
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
    private static bool WriteSimpleValue(BinaryWriter writer, Type type, object value)
    {
        if (type == typeof(bool))
            writer.Write((bool)value);
        else if (type == typeof(byte))
            writer.Write((byte)value);
        else if (type == typeof(sbyte))
            writer.Write((sbyte)value);
        else if (type == typeof(short))
            writer.Write((short)value);
        else if (type == typeof(ushort))
            writer.Write((ushort)value);
        else if (type == typeof(int))
            writer.Write((int)value);
        else if (type == typeof(uint))
            writer.Write((uint)value);
        else if (type == typeof(long))
            writer.Write((long)value);
        else if (type == typeof(ulong))
            writer.Write((ulong)value);
        else if (type == typeof(float))
            writer.Write((float)value);
        else if (type == typeof(double))
            writer.Write((double)value);
        else if (type == typeof(decimal))
            writer.Write((decimal)value);
        else if (type == typeof(char))
            writer.Write((char)value);
        else if (type == typeof(DateTime))
            writer.Write(((DateTime)value).ToBinary());
        else if (type == typeof(TimeSpan))
            writer.Write(((TimeSpan)value).Ticks);
        else if (type == typeof(Vector3))
            writer.Write((Vector3)value);
        else if (type == typeof(Quaternion))
            writer.Write((Quaternion)value);
        else
            return false;
        return true;
    }
    private static bool ReadSimpleValue(BinaryReader reader, Type type, out object value)
    {
        value = null;
        if (type == typeof(bool))
            value = reader.ReadBoolean();
        else if (type == typeof(byte))
            value = reader.ReadByte();
        else if (type == typeof(sbyte))
            value = reader.ReadSByte();
        else if (type == typeof(short))
            value = reader.ReadInt16();
        else if (type == typeof(ushort))
            value = reader.ReadUInt16();
        else if (type == typeof(int))
            value = reader.ReadInt32();
        else if (type == typeof(uint))
            value = reader.ReadUInt32();
        else if (type == typeof(long))
            value = reader.ReadInt64();
        else if (type == typeof(ulong))
            value = reader.ReadUInt64();
        else if (type == typeof(float))
            value = reader.ReadSingle();
        else if (type == typeof(double))
            value = reader.ReadDouble();
        else if (type == typeof(decimal))
            value = reader.ReadDecimal();
        else if (type == typeof(char))
            value = reader.ReadChar();
        else if (type == typeof(DateTime))
            value = DateTime.FromBinary(reader.ReadInt64());
        else if (type == typeof(TimeSpan))
            value = TimeSpan.FromTicks(reader.ReadInt64());
        else if (type == typeof(Vector3))
            value = reader.ReadVector3();
        else if (type == typeof(Quaternion))
            value = reader.ReadQuaternion();
        else
            return false;
        return true;
    }
    public static void Write(this BinaryWriter writer, Vector3 vector)
    {
        writer.Write(vector.x);
        writer.Write(vector.y);
        writer.Write(vector.z);
    }
    public static Vector3 ReadVector3(this BinaryReader reader)
    {
        var x = reader.ReadSingle();
        var y = reader.ReadSingle();
        var z = reader.ReadSingle();
        return new Vector3(x, y, z);
    }
    public static Quaternion ReadQuaternion(this BinaryReader reader)
    {
        var x = reader.ReadSingle();
        var y = reader.ReadSingle();
        var z = reader.ReadSingle();
        var w = reader.ReadSingle();
        return new Quaternion(x, y, z, w);
    }
    public static void Write(this BinaryWriter writer, Quaternion quaternion)
    {
        writer.Write(quaternion.x);
        writer.Write(quaternion.y);
        writer.Write(quaternion.z);
        writer.Write(quaternion.w);
    }
}
