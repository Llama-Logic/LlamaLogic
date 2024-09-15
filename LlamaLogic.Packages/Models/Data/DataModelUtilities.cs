namespace LlamaLogic.Packages.Models.Data;

static class DataModelUtilities
{
    static readonly ImmutableDictionary<DataModelType, DataModelAlignment> alignmentByResourceType = Enum
        .GetValues<DataModelType>()
        .ToImmutableDictionary
        (
            type => type,
            type => typeof(DataModelType)
                .GetMember(type.ToString())
                .FirstOrDefault()
                ?.GetCustomAttribute<DataModelTypeAlignmentAttribute>()
                ?.Alignment
                ?? DataModelAlignment.None
        );
    const uint sixteenByteAlignmentMask = 0xF;

    static void Align(ArrayBufferWriter<byte> writer, uint alignmentMask)
    {
        var position = (uint)writer.WrittenCount;
        var newPosition = Align(position, alignmentMask);
        if (newPosition > position)
            writer.Advance((int)(newPosition - position));
    }

    public static uint Align(this uint position, DataModelAlignment alignment) =>
        Align(position, (uint)alignment - 1);

    static uint Align(uint position, uint alignmentMask) =>
        position + (uint)(-(int)position & alignmentMask);

    public static int AlignForNextBlock(this int position) =>
        (int)Align((uint)position, sixteenByteAlignmentMask);

    public static void AlignForNextBlock(this ArrayBufferWriter<byte> writer) =>
        Align(writer, sixteenByteAlignmentMask);

    public static void AlignForTable(this ArrayBufferWriter<byte> writer, DataModelTable table) =>
        Align(writer, table.RowSize - 1);

    public static DataModelAlignment GetAlignment(this DataModelType type) =>
        alignmentByResourceType.TryGetValue(type, out var alignment) ? alignment : DataModelAlignment.None;

    public static string? GetNullTerminatedString(this ReadOnlySpan<byte> span, uint? position)
    {
        if (position is not { } nonNullPosition)
            return null;
        var iPosition = (int)nonNullPosition;
        int length;
        try
        {
            length = span[iPosition..].IndexOf((byte)0);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to find null terminator in string at position {nonNullPosition} ({nonNullPosition:X}h).", ex);
        }
        try
        {
            return Encoding.ASCII.GetString(span[iPosition..(iPosition + length)]);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to decode string at position {nonNullPosition} ({nonNullPosition:X}h) of length {length} ({length:X}h).", ex);
        }
    }

    public static Memory<byte> GetOffsetMemoryAndAdvance(this ArrayBufferWriter<byte> writer) =>
        writer.GetMemoryAndAdvance(sizeof(int));

    public static Span<byte> GetOffsetSpanAndAdvance(this ArrayBufferWriter<byte> writer) =>
        writer.GetSpanAndAdvance(sizeof(int));

    public static uint GetSize(this DataModelType packageDataResourceType) =>
        packageDataResourceType switch
        {
            DataModelType.BOOL => sizeof(byte),
            DataModelType.CHAR8 => sizeof(byte),
            DataModelType.INT8 => sizeof(sbyte),
            DataModelType.UINT8 => sizeof(byte),
            DataModelType.INT16 => sizeof(short),
            DataModelType.UINT16 => sizeof(ushort),
            DataModelType.INT32 => sizeof(int),
            DataModelType.UINT32 => sizeof(uint),
            DataModelType.INT64 => sizeof(long),
            DataModelType.UINT64 => sizeof(ulong),
            DataModelType.FLOAT => sizeof(float),
            DataModelType.STRING8 => sizeof(int),
            DataModelType.HASHEDSTRING8 => sizeof(int) + sizeof(uint),
            DataModelType.OBJECT => sizeof(int),
            DataModelType.VECTOR => sizeof(int) + sizeof(uint),
            DataModelType.FLOAT2 => sizeof(float) * 2,
            DataModelType.FLOAT3 => sizeof(float) * 3,
            DataModelType.FLOAT4 => sizeof(float) * 4,
            DataModelType.TABLESETREFERENCE => sizeof(ulong),
            DataModelType.RESOURCEKEY => sizeof(ulong) + sizeof(ResourceType) + sizeof(uint),
            DataModelType.LOCKEY => sizeof(uint),
            DataModelType.VARIANT => sizeof(int) + sizeof(uint),
            _ => 0
        };

    public static uint? ReadOffset(this ReadOnlyMemory<byte> memory, uint position)
    {
        var offset = memory.Read<int>(position);
        return offset is BinaryUtilities.NullOffset ? null : (uint)(position + offset);
    }

    public static uint? ReadOffset(this ReadOnlySpan<byte> span, uint position)
    {
        var offset = span.Read<int>(position);
        return offset is BinaryUtilities.NullOffset ? null : (uint)(position + offset);
    }

    public static uint? ReadOffsetAndAdvancePosition(this ReadOnlyMemory<byte> memory, ref uint position)
    {
        var offset = ReadOffset(memory, position);
        position += sizeof(int);
        return offset;
    }

    public static uint? ReadOffsetAndAdvancePosition(this ReadOnlySpan<byte> span, ref uint position)
    {
        var offset = ReadOffset(span, position);
        position += sizeof(int);
        return offset;
    }

    public static long? ReadOffsetAsLongPosition(this ReadOnlySpan<byte> span, uint position)
    {
        var offset = span.Read<int>(position);
        return offset is BinaryUtilities.NullOffset ? null : (position + offset);
    }

    public static long? ReadOffsetAsLongPositionAndAdvancePosition(this ReadOnlySpan<byte> span, ref uint position)
    {
        var offset = ReadOffsetAsLongPosition(span, position);
        position += sizeof(int);
        return offset;
    }

    public static string? ReadOffsetStringAndAdvancePosition(this ReadOnlySpan<byte> span, ref uint position)
    {
        var stringPosition = ReadOffsetAndAdvancePosition(span, ref position);
        return stringPosition is null
            ? null
            : GetNullTerminatedString(span, stringPosition);
    }

    public static string? ReadOffsetHashedStringAndAdvancePosition(this ReadOnlySpan<byte> span, ref uint position)
    {
        var str = ReadOffsetStringAndAdvancePosition(span, ref position);
        position += sizeof(uint);
        return str;
    }

    public static void WriteOffset(this Memory<byte> memory, ArrayBufferWriter<byte> writer, int? toPosition = null) =>
        WriteOffset(memory.Span, writer, toPosition);

    public static void WriteOffset(this Span<byte> span, ArrayBufferWriter<byte> writer, int? toPosition = null)
    {
        if (span.Length != sizeof(int))
            throw new ArgumentException($"The span must be exactly {sizeof(int)} bytes long.", nameof(span));
        ref var spanRef = ref MemoryMarshal.GetReference(span);
        ref var writtenSpanRef = ref MemoryMarshal.GetReference(writer.WrittenSpan);
        var offset = (toPosition ?? writer.WrittenCount) - (int)Unsafe.ByteOffset(ref writtenSpanRef, ref spanRef);
        MemoryMarshal.Write(span, ref offset);
    }
}
