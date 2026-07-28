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
        if (!Serializers.TryGetValue(type, out var serializer))
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
    private static void WriteBooleanField(BinaryWriter writer, float order, bool value)
    {
        Span<byte> bytes = stackalloc byte[9];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, BitConverter.SingleToInt32Bits(order));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(4), 1u);
        bytes[8] = value ? (byte)1 : (byte)0;
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteByteField(BinaryWriter writer, float order, byte value)
    {
        Span<byte> bytes = stackalloc byte[9];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, BitConverter.SingleToInt32Bits(order));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(4), 1u);
        bytes[8] = value;
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteSByteField(BinaryWriter writer, float order, sbyte value)
    {
        Span<byte> bytes = stackalloc byte[9];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, BitConverter.SingleToInt32Bits(order));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(4), 1u);
        bytes[8] = unchecked((byte)value);
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteInt16Field(BinaryWriter writer, float order, short value)
    {
        Span<byte> bytes = stackalloc byte[10];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, BitConverter.SingleToInt32Bits(order));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(4), 2u);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.Slice(8), value);
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteUInt16Field(BinaryWriter writer, float order, ushort value)
    {
        Span<byte> bytes = stackalloc byte[10];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, BitConverter.SingleToInt32Bits(order));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(4), 2u);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.Slice(8), value);
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteInt32Field(BinaryWriter writer, float order, int value)
    {
        Span<byte> bytes = stackalloc byte[12];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, BitConverter.SingleToInt32Bits(order));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(4), 4u);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(8), value);
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteUInt32Field(BinaryWriter writer, float order, uint value)
    {
        Span<byte> bytes = stackalloc byte[12];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, BitConverter.SingleToInt32Bits(order));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(4), 4u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(8), value);
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteInt64Field(BinaryWriter writer, float order, long value)
    {
        Span<byte> bytes = stackalloc byte[16];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, BitConverter.SingleToInt32Bits(order));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(4), 8u);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.Slice(8), value);
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteUInt64Field(BinaryWriter writer, float order, ulong value)
    {
        Span<byte> bytes = stackalloc byte[16];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, BitConverter.SingleToInt32Bits(order));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(4), 8u);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.Slice(8), value);
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteSingleField(BinaryWriter writer, float order, float value)
    {
        Span<byte> bytes = stackalloc byte[12];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, BitConverter.SingleToInt32Bits(order));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(4), 4u);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(8), BitConverter.SingleToInt32Bits(value));
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteDoubleField(BinaryWriter writer, float order, double value)
    {
        Span<byte> bytes = stackalloc byte[16];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, BitConverter.SingleToInt32Bits(order));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(4), 8u);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.Slice(8), BitConverter.DoubleToInt64Bits(value));
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteDecimalField(BinaryWriter writer, float order, decimal value)
    {
        Span<byte> bytes = stackalloc byte[24];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, BitConverter.SingleToInt32Bits(order));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(4), 16u);
        var bits = decimal.GetBits(value);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(8), bits[0]);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(12), bits[1]);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(16), bits[2]);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(20), bits[3]);
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteDateTimeField(BinaryWriter writer, float order, DateTime value) => WriteInt64Field(writer, order, value.ToBinary());
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteTimeSpanField(BinaryWriter writer, float order, TimeSpan value) => WriteInt64Field(writer, order, value.Ticks);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteVector3Field(BinaryWriter writer, float order, Vector3 value)
    {
        Span<byte> bytes = stackalloc byte[20];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, BitConverter.SingleToInt32Bits(order));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(4), 12u);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(8), BitConverter.SingleToInt32Bits(value.x));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(12), BitConverter.SingleToInt32Bits(value.y));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(16), BitConverter.SingleToInt32Bits(value.z));
        writer.BaseStream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteQuaternionField(BinaryWriter writer, float order, Quaternion value)
    {
        Span<byte> bytes = stackalloc byte[24];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, BitConverter.SingleToInt32Bits(order));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(4), 16u);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(8), BitConverter.SingleToInt32Bits(value.x));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(12), BitConverter.SingleToInt32Bits(value.y));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(16), BitConverter.SingleToInt32Bits(value.z));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(20), BitConverter.SingleToInt32Bits(value.w));
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

    private static void WriteStringField(BinaryWriter writer, float order, string value)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        var serializedLength = Get7BitEncodedIntSize(byteCount) + byteCount;
        Span<byte> header = stackalloc byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(header, BitConverter.SingleToInt32Bits(order));
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(4), checked((uint)serializedLength));
        writer.BaseStream.Write(header);
        writer.Write(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteObjectReferencesField(BinaryWriter writer, float order, ObjectReferences value)
    {
        var count = value.IDs.Count;
        if ((uint)count > byte.MaxValue)
            throw new InvalidDataException($"ObjectReferences cannot contain more than {byte.MaxValue} IDs.");
        var nestedLength = 10 + count * sizeof(ulong);
        var totalLength = 8 + nestedLength;
        Span<byte> bytes = stackalloc byte[totalLength];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, BitConverter.SingleToInt32Bits(order));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(4), checked((uint)nestedLength));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(8), 0u);
        bytes[12] = 1;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(13), checked((uint)(1 + count * sizeof(ulong))));
        bytes[17] = checked((byte)count);
        var offset = 18;
        foreach (var id in value.IDs)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(bytes.Slice(offset), id);
            offset += sizeof(ulong);
        }
        writer.BaseStream.Write(bytes);
    }
}
