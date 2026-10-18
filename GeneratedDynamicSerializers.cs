using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Salmon.Levels;

namespace Salmon;

internal static partial class GeneratedDynamicSerializers
{
    private delegate void WriteDelegate(object value, BinaryWriter writer);
    private delegate object ReadDelegate(BinaryReader reader);
    private static readonly Dictionary<Type, (WriteDelegate Write, ReadDelegate Read)> Serializers = new();
    private static void Register(Type type, WriteDelegate write, ReadDelegate read) =>
        Serializers.Add(type, (write, read));


    public static bool TryWrite(object value, BinaryWriter writer)
    {
        if (value == null || !Serializers.TryGetValue(value.GetType(), out var serializer))
            return false;

        serializer.Write(value, writer);
        return true;
    }

    public static bool TryRead(Type type, BinaryReader reader, out object value)
    {
        if (DynamicSerializer.OldRead || !Serializers.TryGetValue(type, out var serializer))
        {
            value = null;
            return false;
        }

        value = serializer.Read(reader);
        return true;
    }
    private static byte Clamp(byte value, float min, float max)
    {
        if (!float.IsNaN(min) && value < min) value = (byte)min;
        if (!float.IsNaN(max) && value > max) value = (byte)max;
        return value;
    }

    private static sbyte Clamp(sbyte value, float min, float max)
    {
        if (!float.IsNaN(min) && value < min) value = (sbyte)min;
        if (!float.IsNaN(max) && value > max) value = (sbyte)max;
        return value;
    }

    private static short Clamp(short value, float min, float max)
    {
        if (!float.IsNaN(min) && value < min) value = (short)min;
        if (!float.IsNaN(max) && value > max) value = (short)max;
        return value;
    }

    private static ushort Clamp(ushort value, float min, float max)
    {
        if (!float.IsNaN(min) && value < min) value = (ushort)min;
        if (!float.IsNaN(max) && value > max) value = (ushort)max;
        return value;
    }

    private static int Clamp(int value, float min, float max)
    {
        if (!float.IsNaN(min) && value < min) value = (int)min;
        if (!float.IsNaN(max) && value > max) value = (int)max;
        return value;
    }

    private static uint Clamp(uint value, float min, float max)
    {
        if (!float.IsNaN(min) && value < min) value = (uint)min;
        if (!float.IsNaN(max) && value > max) value = (uint)max;
        return value;
    }

    private static long Clamp(long value, float min, float max)
    {
        if (!float.IsNaN(min) && value < min) value = (long)min;
        if (!float.IsNaN(max) && value > max) value = (long)max;
        return value;
    }

    private static ulong Clamp(ulong value, float min, float max)
    {
        if (!float.IsNaN(min) && value < min) value = (ulong)min;
        if (!float.IsNaN(max) && value > max) value = (ulong)max;
        return value;
    }

    private static float Clamp(float value, float min, float max)
    {
        if (!float.IsNaN(min) && value < min) value = min;
        if (!float.IsNaN(max) && value > max) value = max;
        return value;
    }

    private static double Clamp(double value, float min, float max)
    {
        if (!float.IsNaN(min) && value < min) value = min;
        if (!float.IsNaN(max) && value > max) value = max;
        return value;
    }

    private static decimal Clamp(decimal value, float min, float max)
    {
        if (!float.IsNaN(min) && value < (decimal)min) value = (decimal)min;
        if (!float.IsNaN(max) && value > (decimal)max) value = (decimal)max;
        return value;
    }

    private static Vector3 Clamp(Vector3 value, float min, float max)
    {
        if (!float.IsNaN(min) && value.x < min) value.x = min;
        if (!float.IsNaN(max) && value.x > max) value.x = max;
        if (!float.IsNaN(min) && value.y < min) value.y = min;
        if (!float.IsNaN(max) && value.y > max) value.y = max;
        if (!float.IsNaN(min) && value.z < min) value.z = min;
        if (!float.IsNaN(max) && value.z > max) value.z = max;
        return value;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteBooleanField(BinaryWriter writer, byte id, bool value)
    {
        Span<byte> bytes = stackalloc byte[6];
        bytes[0] = id;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(1), 1u);
        bytes[5] = value ? (byte)1 : (byte)0;
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteByteField(BinaryWriter writer, byte id, byte value)
    {
        Span<byte> bytes = stackalloc byte[6];
        bytes[0] = id;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(1), 1u);
        bytes[5] = value;
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteSByteField(BinaryWriter writer, byte id, sbyte value)
    {
        Span<byte> bytes = stackalloc byte[6];
        bytes[0] = id;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(1), 1u);
        bytes[5] = unchecked((byte)value);
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteInt16Field(BinaryWriter writer, byte id, short value)
    {
        Span<byte> bytes = stackalloc byte[7];
        bytes[0] = id;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(1), 2u);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.Slice(5), value);
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteUInt16Field(BinaryWriter writer, byte id, ushort value)
    {
        Span<byte> bytes = stackalloc byte[7];
        bytes[0] = id;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(1), 2u);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.Slice(5), value);
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteInt32Field(BinaryWriter writer, byte id, int value)
    {
        Span<byte> bytes = stackalloc byte[9];
        bytes[0] = id;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(1), 4u);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(5), value);
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteUInt32Field(BinaryWriter writer, byte id, uint value)
    {
        Span<byte> bytes = stackalloc byte[9];
        bytes[0] = id;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(1), 4u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(5), value);
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteInt64Field(BinaryWriter writer, byte id, long value)
    {
        Span<byte> bytes = stackalloc byte[13];
        bytes[0] = id;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(1), 8u);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.Slice(5), value);
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteUInt64Field(BinaryWriter writer, byte id, ulong value)
    {
        Span<byte> bytes = stackalloc byte[13];
        bytes[0] = id;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(1), 8u);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.Slice(5), value);
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteSingleField(BinaryWriter writer, byte id, float value)
    {
        Span<byte> bytes = stackalloc byte[9];
        bytes[0] = id;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(1), 4u);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(5), BitConverter.SingleToInt32Bits(value));
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteDoubleField(BinaryWriter writer, byte id, double value)
    {
        Span<byte> bytes = stackalloc byte[13];
        bytes[0] = id;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(1), 8u);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.Slice(5), BitConverter.DoubleToInt64Bits(value));
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteDecimalField(BinaryWriter writer, byte id, decimal value)
    {
        Span<byte> bytes = stackalloc byte[21];
        bytes[0] = id;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(1), 16u);
        var bits = decimal.GetBits(value);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(5), bits[0]);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(9), bits[1]);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(13), bits[2]);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(17), bits[3]);
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteDateTimeField(BinaryWriter writer, byte id, DateTime value) => WriteInt64Field(writer, id, value.ToBinary());
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteTimeSpanField(BinaryWriter writer, byte id, TimeSpan value) => WriteInt64Field(writer, id, value.Ticks);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteVector3Field(BinaryWriter writer, byte id, Vector3 value)
    {
        Span<byte> bytes = stackalloc byte[17];
        bytes[0] = id;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(1), 12u);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(5), BitConverter.SingleToInt32Bits(value.x));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(9), BitConverter.SingleToInt32Bits(value.y));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(13), BitConverter.SingleToInt32Bits(value.z));
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteQuaternionField(BinaryWriter writer, byte id, Quaternion value)
    {
        Span<byte> bytes = stackalloc byte[21];
        bytes[0] = id;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(1), 16u);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(5), BitConverter.SingleToInt32Bits(value.x));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(9), BitConverter.SingleToInt32Bits(value.y));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(13), BitConverter.SingleToInt32Bits(value.z));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(17), BitConverter.SingleToInt32Bits(value.w));
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Get7BitEncodedIntSize(int value)
    {
        if ((uint)value < 0x80u) return 1;
        if ((uint)value < 0x4000u) return 2;
        if ((uint)value < 0x200000u) return 3;
        if ((uint)value < 0x10000000u) return 4;
        return 5;
    }

    private static void WriteStringField(BinaryWriter writer, byte id, string value)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        var serializedLength = Get7BitEncodedIntSize(byteCount) + byteCount;
        Span<byte> header = stackalloc byte[5];
        header[0] = id;
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(1), checked((uint)serializedLength));
        writer.BaseStream.Write(header);
        writer.Write(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteObjectReferencesField(BinaryWriter writer, byte id, ObjectReferences value)
    {
        var count = value.IDs.Count;
        if ((uint)count > byte.MaxValue)
            throw new InvalidDataException($"ObjectReferences cannot contain more than {byte.MaxValue} IDs.");
        var nestedLength = 10 + count * sizeof(ulong);
        var totalLength = 5 + nestedLength;
        Span<byte> bytes = stackalloc byte[totalLength];
        bytes[0] = id;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(1), checked((uint)nestedLength));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(5), 0u);
        bytes[9] = 1;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(10), checked((uint)(1 + count * sizeof(ulong))));
        bytes[14] = checked((byte)count);
        var offset = 15;
        foreach (var objectID in value.IDs)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(bytes.Slice(offset), objectID);
            offset += sizeof(ulong);
        }
        writer.BaseStream.Write(bytes);
    }
}
