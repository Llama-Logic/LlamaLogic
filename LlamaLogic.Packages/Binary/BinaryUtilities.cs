namespace LlamaLogic.Packages.Binary;

static class BinaryUtilities
{
    const uint sixteenByteAlignmentMask = 0xF;
    static readonly ConcurrentDictionary<Type, int> typeSizes = new();

    public const byte FalseByte = 0;
    public const uint NullOffset = 0x80000000;
    public const byte TrueByte = 1;

    public static Alignment AggregateAlignment(this Alignment alignment, DataResourceType type)
    {
        var typeAlignment = GetAlignment(type);
        if (typeAlignment > alignment)
            return typeAlignment;
        return alignment;
    }

    static void Align(ArrayBufferWriter<byte> writer, uint alignmentMask)
    {
        var position = (uint)writer.WrittenCount;
        var newPosition = Align(position, alignmentMask);
        if (newPosition > position)
            writer.Advance((int)(newPosition - position));
    }

    static uint Align(uint position, uint alignmentMask) =>
        position + (uint)(-(int)position & alignmentMask);

    public static void AlignForFirstRow(this ArrayBufferWriter<byte> writer, out Alignment alignment)
    {
        alignment = Alignment.None;
        Align(writer, sixteenByteAlignmentMask);
    }

    public static uint AlignForNextRow(this uint position, ref Alignment alignment)
    {
        var newPosition = Align(position, (uint)alignment - 1);
        alignment = Alignment.None;
        return newPosition;
    }

    public static Alignment GetAlignment(this DataResourceType type) =>
        type switch
        {
            DataResourceType.ShortInteger
         or DataResourceType.UnsignedShortInteger =>
             Alignment.TwoBytes,
            DataResourceType.Integer
         or DataResourceType.UnsignedInteger
         or DataResourceType.FloatingPoint
         or DataResourceType.String
         or DataResourceType.HashedString
         or DataResourceType.Object
         or DataResourceType.Vector
         or DataResourceType.FloatingPoint2
         or DataResourceType.FloatingPoint3
         or DataResourceType.FloatingPoint4
         or DataResourceType.LocalizationKey =>
             Alignment.FourBytes,
            DataResourceType.LongInteger
         or DataResourceType.UnsignedLongInteger
         or DataResourceType.TableSetReference
         or DataResourceType.ResourceKey =>
             Alignment.EightBytes,
            _ =>
                Alignment.None
        };

    public static string? GetNullTerminatedString(this ReadOnlySpan<byte> span, uint? position)
    {
        if (position is not { } nonNullPosition)
            return null;
        var iPosition = (int)nonNullPosition;
        return Encoding.ASCII.GetString(span[iPosition..(span[iPosition..].IndexOf((byte)0) + iPosition)]);
    }

    public static uint GetOffsetValueFromCurrentPosition(this ArrayBufferWriter<byte> writer, int? offsetPosition) =>
        offsetPosition is { } nonNullOffsetPosition
        ? (uint)writer.WrittenCount - (uint)nonNullOffsetPosition
        : NullOffset;

    public static uint GetOffsetValueFromCurrentPosition(this ArrayBufferWriter<byte> writer, in Range structRange, int inStructOffset) =>
        GetOffsetValueFromCurrentPosition(writer, structRange.GetOffsetAndLength(writer.WrittenCount).Offset + inStructOffset);

    public static uint GetSize(this DataResourceType packageDataResourceType) =>
        packageDataResourceType switch
        {
            DataResourceType.Boolean => sizeof(byte),
            DataResourceType.Character => sizeof(byte),
            DataResourceType.SignedByte => sizeof(sbyte),
            DataResourceType.Byte => sizeof(byte),
            DataResourceType.ShortInteger => sizeof(short),
            DataResourceType.UnsignedShortInteger => sizeof(ushort),
            DataResourceType.Integer => sizeof(int),
            DataResourceType.UnsignedInteger => sizeof(uint),
            DataResourceType.LongInteger => sizeof(long),
            DataResourceType.UnsignedLongInteger => sizeof(ulong),
            DataResourceType.FloatingPoint => sizeof(float),
            DataResourceType.String => sizeof(uint),
            DataResourceType.HashedString => sizeof(uint) + sizeof(uint),
            DataResourceType.Object => sizeof(uint),
            DataResourceType.Vector => sizeof(uint) + sizeof(uint),
            DataResourceType.FloatingPoint2 => sizeof(float) * 2,
            DataResourceType.FloatingPoint3 => sizeof(float) * 3,
            DataResourceType.FloatingPoint4 => sizeof(float) * 4,
            DataResourceType.TableSetReference => sizeof(ulong),
            DataResourceType.ResourceKey => sizeof(ulong) + sizeof(uint) + sizeof(uint),
            DataResourceType.LocalizationKey => sizeof(uint),
            _ => 0
        };

    public static Span<byte> GetSpanAndAdvance(this ArrayBufferWriter<byte> writer, int count)
    {
        var span = writer.GetSpan(count)[..count];
        writer.Advance(count);
        return span;
    }

    public static Span<byte> GetSpanAndAdvance<T>(this ArrayBufferWriter<byte> writer)
        where T : struct =>
        GetSpanAndAdvance(writer, typeSizes.GetOrAdd(typeof(T), TypeSizeValueFactory));

    public static bool IsNullOffset(this uint offset) =>
        offset is NullOffset;

    public static T ReadAndAdvancePosition<T>(this ReadOnlySpan<byte> readOnlySpan, ref uint position)
        where T : struct
    {
        var size = typeSizes.GetOrAdd(typeof(T), TypeSizeValueFactory);
        var iPosition = (int)position;
        var result = MemoryMarshal.Read<T>(readOnlySpan[iPosition..(iPosition + size)]);
        position += (uint)size;
        return result;
    }

    public static T ReadAndAdvancePosition<T>(this Span<byte> span, ref uint position)
        where T : struct
    {
        var size = typeSizes.GetOrAdd(typeof(T), TypeSizeValueFactory);
        var iPosition = (int)position;
        var result = MemoryMarshal.Read<T>(span[iPosition..(iPosition + size)]);
        position += (uint)size;
        return result;
    }

    public static unsafe void ReadBinaryStruct<T>(this ReadOnlySpan<byte> readOnlySpan, out T binaryStruct)
        where T : struct, IBinaryStruct
    {
        var size = typeSizes.GetOrAdd(typeof(T), TypeSizeValueFactory);
        var slice = readOnlySpan[..size];
        fixed (byte* ptr = slice)
            binaryStruct = Marshal.PtrToStructure<T>((IntPtr)ptr);
    }

    public static unsafe void ReadBinaryStructAndAdvancePosition<T>(this ReadOnlySpan<byte> readOnlySpan, out T binaryStruct, ref uint position)
        where T : struct, IBinaryStruct
    {
        var size = typeSizes.GetOrAdd(typeof(T), TypeSizeValueFactory);
        var iPosition = (int)position;
        var slice = readOnlySpan[iPosition..(iPosition + size)];
        fixed (byte* ptr = slice)
            binaryStruct = Marshal.PtrToStructure<T>((IntPtr)ptr);
        position += (uint)size;
    }

    public static void ReserveMemoryForBinaryStruct<T>(this ArrayBufferWriter<byte> writer, out Memory<byte> reservedMemory, out Range reservedMemoryRange)
        where T : struct, IBinaryStruct
    {
        var currentPosition = writer.WrittenCount;
        var size = typeSizes.GetOrAdd(typeof(T), TypeSizeValueFactory);
        reservedMemory = writer.GetMemory(size)[..size];
        reservedMemoryRange = new Range(currentPosition, currentPosition + size);
        writer.Advance(size);
    }

    public static void ReserveSpanForBinaryStruct<T>(this ArrayBufferWriter<byte> writer, out Span<byte> reservedSpan, out Range reservedSpanRange)
        where T : struct, IBinaryStruct
    {
        var currentPosition = writer.WrittenCount;
        var size = typeSizes.GetOrAdd(typeof(T), TypeSizeValueFactory);
        reservedSpan = writer.GetSpan(size)[..size];
        reservedSpanRange = new Range(currentPosition, currentPosition + size);
        writer.Advance(size);
    }

    public static int TypeSizeValueFactory(Type type)
    {
        if (type.IsEnum)
            type = type.GetEnumUnderlyingType();
        return Marshal.SizeOf(type);
    }

    public static void Write<T>(this ArrayBufferWriter<byte> writer, ref T value)
        where T : struct =>
        MemoryMarshal.Write(writer.GetSpanAndAdvance<T>(), ref value);

    public static unsafe void WriteBinaryStruct<T>(this ArrayBufferWriter<byte> writer, in T binaryStruct)
        where T : struct, IBinaryStruct
    {
        var size = typeSizes.GetOrAdd(typeof(T), TypeSizeValueFactory);
        fixed (byte* ptr = writer.GetSpan(size)[..size])
            Marshal.StructureToPtr(binaryStruct, (IntPtr)ptr, false);
        writer.Advance(size);
    }

    public static unsafe void WriteBinaryStruct<T>(this Memory<byte> memory, in T binaryStruct)
        where T : struct, IBinaryStruct
    {
        fixed (byte* ptr = memory.Span)
            Marshal.StructureToPtr(binaryStruct, (IntPtr)ptr, false);
    }

    public static unsafe void WriteBinaryStruct<T>(this Span<byte> span, in T binaryStruct)
        where T : struct, IBinaryStruct
    {
        fixed (byte* ptr = span)
            Marshal.StructureToPtr(binaryStruct, (IntPtr)ptr, false);
    }

    public static void WriteIndexComponent(this Stream stream, ref BinaryIndexEntry binaryIndexEntry) =>
        binaryIndexEntry.Position = (uint)stream.Position;
}
