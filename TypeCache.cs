using System.Reflection;
namespace Salmon;

internal sealed class DynamicInspectorField(float order, Type valueType, InspectorFieldAttribute attribute, Func<object, object> getValue, Action<object, object> setValue)
{
    public string Label => Attribute.Label;
    public readonly float Order = order;
    public readonly Type ValueType = valueType;
    public readonly InspectorFieldAttribute Attribute = attribute;
    public readonly Func<object, object> GetValue = getValue;
    public readonly Action<object, object> SetValue = setValue;
}

internal sealed class DynamicInspectorButton(float order, InspectorButtonAttribute attribute, DynamicInspectorButton.DynamicInspectorButtonDelegate onClick)
{
    public delegate void DynamicInspectorButtonDelegate(object target);
    public string Label => Attribute.Label;
    public readonly float Order = order;
    public readonly InspectorButtonAttribute Attribute = attribute;
    public readonly DynamicInspectorButtonDelegate OnClick = onClick;
}
internal sealed class DynamicInspectorType(DynamicInspectorField[] fields, DynamicInspectorButton[] buttons, Dictionary<float, DynamicInspectorField> fieldsByOrder, Dictionary<InspectorHandleType, DynamicInspectorField> fieldsByHandle)
{
    public readonly DynamicInspectorField[] Fields = fields;
    public readonly DynamicInspectorButton[] Buttons = buttons;
    public readonly Dictionary<float, DynamicInspectorField> FieldsByOrder = fieldsByOrder;
    public readonly Dictionary<InspectorHandleType, DynamicInspectorField> FieldsByHandle = fieldsByHandle;
}
internal static class TypeCache
{
    public static Dictionary<Type, DynamicInspectorType> Types = new();
    private static object ClampNumber(object value, Type type, float min, float max)
    {
        var hasMin = !float.IsNaN(min);
        var hasMax = !float.IsNaN(max);

        if (type == typeof(byte))
        {
            var v = (byte)value;
            if (hasMin && v < min) v = (byte)min;
            if (hasMax && v > max) v = (byte)max;
            return v;
        }

        if (type == typeof(sbyte))
        {
            var v = (sbyte)value;
            if (hasMin && v < min) v = (sbyte)min;
            if (hasMax && v > max) v = (sbyte)max;
            return v;
        }

        if (type == typeof(short))
        {
            var v = (short)value;
            if (hasMin && v < min) v = (short)min;
            if (hasMax && v > max) v = (short)max;
            return v;
        }

        if (type == typeof(ushort))
        {
            var v = (ushort)value;
            if (hasMin && v < min) v = (ushort)min;
            if (hasMax && v > max) v = (ushort)max;
            return v;
        }

        if (type == typeof(int))
        {
            var v = (int)value;
            if (hasMin && v < min) v = (int)min;
            if (hasMax && v > max) v = (int)max;
            return v;
        }

        if (type == typeof(uint))
        {
            var v = (uint)value;
            if (hasMin && v < min) v = (uint)min;
            if (hasMax && v > max) v = (uint)max;
            return v;
        }

        if (type == typeof(long))
        {
            var v = (long)value;
            if (hasMin && v < min) v = (long)min;
            if (hasMax && v > max) v = (long)max;
            return v;
        }

        if (type == typeof(ulong))
        {
            var v = (ulong)value;
            if (hasMin && v < min) v = (ulong)min;
            if (hasMax && v > max) v = (ulong)max;
            return v;
        }

        if (type == typeof(float))
        {
            var v = (float)value;
            if (hasMin && v < min) v = min;
            if (hasMax && v > max) v = max;
            return v;
        }

        if (type == typeof(double))
        {
            var v = (double)value;
            if (hasMin && v < min) v = min;
            if (hasMax && v > max) v = max;
            return v;
        }

        if (type == typeof(decimal))
        {
            var v = (decimal)value;
            if (hasMin && v < (decimal)min) v = (decimal)min;
            if (hasMax && v > (decimal)max) v = (decimal)max;
            return v;
        }

        if (type == typeof(Vector3))
        {
            var v = (Vector3)value;
            if (hasMin && v.x < min) v.x = min;
            if (hasMax && v.x > max) v.x = max;
            if (hasMin && v.y < min) v.y = min;
            if (hasMax && v.y > max) v.y = max;
            if (hasMin && v.z < min) v.z = min;
            if (hasMax && v.z > max) v.z = max;
            return v;
        }

        return value;
    }
    public static DynamicInspectorType Get(Type type)
    {
        if (Types.TryGetValue(type, out var serializerType))
            return serializerType;
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
            if (attribute == null)
                continue; if (!float.IsNaN(attribute.Min) || !float.IsNaN(attribute.Max))
            {
                dynamicFields.Add(new DynamicInspectorField(attribute.Order, field.FieldType, attribute,
                    obj => ClampNumber(field.GetValue(obj), field.FieldType, attribute.Min, attribute.Max),
                    (obj, value) => field.SetValue(obj, ClampNumber(value, field.FieldType, attribute.Min, attribute.Max))
                ));
            }
            else
            {
                dynamicFields.Add(new DynamicInspectorField(attribute.Order, field.FieldType, attribute, field.GetValue, field.SetValue));
            }
        }
        var properties = type.GetProperties(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic
        );
        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute<InspectorFieldAttribute>();
            if (attribute == null || property.GetIndexParameters().Length != 0 || !property.CanRead || !property.CanWrite)
                continue;
            if (!float.IsNaN(attribute.Min) || !float.IsNaN(attribute.Max))
            {
                dynamicFields.Add(new DynamicInspectorField(attribute.Order, property.PropertyType, attribute,
                    obj => ClampNumber(property.GetValue(obj), property.PropertyType, attribute.Min, attribute.Max),
                    (obj, value) => property.SetValue(obj, ClampNumber(value, property.PropertyType, attribute.Min, attribute.Max))
                ));
            }
            else
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
        var fieldsByOrder = new Dictionary<float, DynamicInspectorField>(dynamicFields.Count);
        var fieldsByHandle = new Dictionary<InspectorHandleType, DynamicInspectorField>(dynamicFields.Count);
        foreach (var field in dynamicFields)
        {
            fieldsByOrder[field.Order] = field;
            if (field.Attribute.Handle != InspectorHandleType.None)
                fieldsByHandle[field.Attribute.Handle] = field;
        }
        serializerType = new DynamicInspectorType(dynamicFields.OrderBy(field => field.Order).ToArray(), dynamicButtons.OrderBy(button => button.Order).ToArray(), fieldsByOrder, fieldsByHandle);
        Types[type] = serializerType;
        return serializerType;
    }
}
