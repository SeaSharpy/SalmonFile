using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Reflection;
namespace Salmon;

internal sealed class DynamicInspectorField(uint order, Type valueType, InspectorFieldAttribute attribute, Func<object, object> getValue, Action<object, object> setValue)
{
    public string Label => Attribute.Label;
    public readonly uint Order = order;
    public readonly Type ValueType = valueType;
    public readonly InspectorFieldAttribute Attribute = attribute;
    public object GetValue(object target) => TypeCache.ClampNumber(getValue(target), ValueType, Attribute.Min, Attribute.Max);
    public void SetValue(object target, object value) => setValue(target, TypeCache.ClampNumber(value, ValueType, Attribute.Min, Attribute.Max));
}

internal sealed class DynamicInspectorButton(float order, InspectorButtonAttribute attribute, Action<object> onClick)
{
    public string Label => Attribute.Label;
    public readonly float Order = order;
    public readonly InspectorButtonAttribute Attribute = attribute;
    public readonly Action<object> OnClick = onClick;
}
internal sealed class UnmanagedSerializer(Func<object, Array> wrap, Action<BinaryWriter, Array> write, Func<BinaryReader, int, Array> read)
{
    public readonly Func<object, Array> Wrap = wrap;
    public readonly Action<BinaryWriter, Array> Write = write;
    public readonly Func<BinaryReader, int, Array> Read = read;
}
internal sealed class DynamicInspectorType(DynamicInspectorField[] fields, DynamicInspectorButton[] buttons, Dictionary<float, DynamicInspectorField> fieldsByOldOrder, Dictionary<byte, DynamicInspectorField> fieldsByID, Dictionary<InspectorHandleType, DynamicInspectorField> fieldsByHandle, UnmanagedSerializer unmanaged)
{
    public readonly UnmanagedSerializer Unmanaged = unmanaged;
    public readonly DynamicInspectorField[] Fields = fields;
    public readonly DynamicInspectorButton[] Buttons = buttons;
    public readonly Dictionary<float, DynamicInspectorField> FieldsByOldOrder = fieldsByOldOrder;
    public readonly Dictionary<byte, DynamicInspectorField> FieldsByID = fieldsByID;
    public readonly Dictionary<InspectorHandleType, DynamicInspectorField> FieldsByHandle = fieldsByHandle;
}
internal static class TypeCache
{
    private static UnmanagedSerializer CreateUnmanagedSerializer<T>() where T : struct
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            return null;
        var size = MemoryMarshal.AsBytes(new T[1].AsSpan()).Length;
        return new UnmanagedSerializer(value => new T[] { (T)value },
            (writer, values) => writer.Write(MemoryMarshal.AsBytes(((T[])values).AsSpan())),
            (reader, count) =>
            {
                if (count < 0 || (long)count * size > reader.BaseStream.Length - reader.BaseStream.Position)
                    throw new InvalidDataException("Unmanaged data exceeds the available bytes.");
                var values = new T[count];
                var remaining = MemoryMarshal.AsBytes(values.AsSpan());
                while (!remaining.IsEmpty)
                {
                    var read = reader.BaseStream.Read(remaining);
                    if (read == 0)
                        throw new EndOfStreamException();
                    remaining = remaining[read..];
                }
                return values;
            });
    }

    [ThreadStatic]
    private static Dictionary<Type, DynamicInspectorType> Types;
    internal static object ClampNumber(object value, Type type, float min, float max)
    {
        if (value == null || type != value.GetType() || float.IsNaN(min) && float.IsNaN(max))
            return value;
        if (value is Vector3 vector)
            return new Vector3((float)ClampNumber(vector.x, typeof(float), min, max),
                (float)ClampNumber(vector.y, typeof(float), min, max), (float)ClampNumber(vector.z, typeof(float), min, max));
        if (value is decimal number)
        {
            if (!float.IsNaN(min) && number < (decimal)min)
                number = (decimal)min;
            if (!float.IsNaN(max) && number > (decimal)max)
                number = (decimal)max;
            return number;
        }
        if (value is not (byte or sbyte or short or ushort or int or uint or long or ulong or float or double))
            return value;
        if (Number(value) < min)
            value = CastNumber(min, type);
        if (Number(value) > max)
            value = CastNumber(max, type);
        return value;
    }
    private static double Number(object value) => value is double number ? number : Convert.ToSingle(value);
    private static object CastNumber(float value, Type type) => Type.GetTypeCode(type) switch
    {
        TypeCode.Byte => (object)(byte)value,
        TypeCode.SByte => (sbyte)value,
        TypeCode.Int16 => (short)value,
        TypeCode.UInt16 => (ushort)value,
        TypeCode.Int32 => (int)value,
        TypeCode.UInt32 => (uint)value,
        TypeCode.Int64 => (long)value,
        TypeCode.UInt64 => (ulong)value,
        TypeCode.Single => value,
        TypeCode.Double => (double)value,
        _ => throw new ArgumentException("Unsupported numeric type.", "type")
    };
    public static DynamicInspectorType Get(Type type)
    {
        var types = Types ??= [];
        if (types.TryGetValue(type, out var serializerType))
            return serializerType;
        serializerType = Create(type);
        types[type] = serializerType;
        return serializerType;
    }
    private static DynamicInspectorType Create(Type type)
    {
        List<DynamicInspectorField> dynamicFields = new List<DynamicInspectorField>();
        List<DynamicInspectorButton> dynamicButtons = new List<DynamicInspectorButton>();
        var fields = type.GetFields(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic
        );
        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttribute<InspectorFieldAttribute>();
            if (attribute != null)
                dynamicFields.Add(new DynamicInspectorField(attribute.Order, field.FieldType, attribute, field.GetValue, field.SetValue));
        }
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var attribute = property.GetCustomAttribute<InspectorFieldAttribute>();
            if (attribute != null && property.GetIndexParameters().Length == 0 && property.CanRead && property.CanWrite)
                dynamicFields.Add(new DynamicInspectorField(attribute.Order, property.PropertyType, attribute, property.GetValue, property.SetValue));
        }
        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        foreach (var method in methods)
        {
            var attribute = method.GetCustomAttribute<InspectorButtonAttribute>();
            if (attribute == null)
                continue;
            dynamicButtons.Add(new DynamicInspectorButton(attribute.Order, attribute, target => method.Invoke(target, [])));
        }
        var fieldsByOldOrder = new Dictionary<float, DynamicInspectorField>(dynamicFields.Count);
        var fieldsByID = new Dictionary<byte, DynamicInspectorField>(dynamicFields.Count);
        var fieldsByHandle = new Dictionary<InspectorHandleType, DynamicInspectorField>(dynamicFields.Count);
        foreach (var field in dynamicFields)
        {
            if (!fieldsByID.TryAdd(field.Attribute.ID, field))
                throw new InvalidOperationException($"Duplicate inspector field ID {field.Attribute.ID} on {type.FullName}.");
            if (!float.IsNaN(field.Attribute.OldOrder) && !fieldsByOldOrder.TryAdd(field.Attribute.OldOrder, field))
                throw new InvalidOperationException($"Duplicate legacy inspector order {field.Attribute.OldOrder} on {type.FullName}.");
            if (field.Attribute.Handle != InspectorHandleType.None)
                fieldsByHandle[field.Attribute.Handle] = field;
        }
        var unmanaged = type.IsValueType && !type.IsPrimitive && !type.IsEnum && type != typeof(decimal) && type != typeof(DateTime) &&
            Nullable.GetUnderlyingType(type) == null && !typeof(ISpecialSerializable).IsAssignableFrom(type) && dynamicFields.Count == 0
                ? (UnmanagedSerializer)typeof(TypeCache).GetMethod(nameof(CreateUnmanagedSerializer), BindingFlags.Static | BindingFlags.NonPublic)
                    .MakeGenericMethod(type).Invoke(null, null)
                : null;
        return new DynamicInspectorType(dynamicFields.OrderBy(field => field.Order).ToArray(), dynamicButtons.OrderBy(button => button.Order).ToArray(), fieldsByOldOrder, fieldsByID, fieldsByHandle, unmanaged);
    }
}
