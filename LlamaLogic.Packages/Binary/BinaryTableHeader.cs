namespace LlamaLogic.Packages.Binary;

class BinaryTableHeader :
    IStringOffsetContainer
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Structured :
        IBinaryStruct
    {
        public uint NameOffset;
        public uint NameHash;
        public uint SchemaOffset;
        public uint Type;
        public uint RowSize;
        public uint RowsOffset;
        public uint RowCount;
    }

    static void CreateForSerialization(out BinaryTableHeader tableHeader)
    {
        tableHeader = new()
        {
            NameOffset = BinaryUtilities.NullOffset,
            NameHash = Fnv32.EmptyStringHash,
            SchemaOffset = BinaryUtilities.NullOffset,
            RowsOffset = BinaryUtilities.NullOffset
        };
    }

    public static void CreateForSerializationAsMemory(ArrayBufferWriter<byte> writer, out BinaryTableHeader tableHeader, out Memory<byte> tableHeaderMemory, out Range tableHeaderMemoryRange)
    {
        CreateForSerialization(out tableHeader);
        writer.ReserveMemoryForBinaryStruct<Structured>(out tableHeaderMemory, out tableHeaderMemoryRange);
    }

    public static void CreateForSerializationAsSpan(ArrayBufferWriter<byte> writer, out BinaryTableHeader tableHeader, out Span<byte> tableHeaderSpan, out Range tableHeaderSpanRange)
    {
        CreateForSerialization(out tableHeader);
        writer.ReserveSpanForBinaryStruct<Structured>(out tableHeaderSpan, out tableHeaderSpanRange);
    }

    public static BinaryTableHeader Read(in ReadOnlySpan<byte> data, ref uint position, out string? name, out uint? schemaPosition, out DataResourceType type, out uint rowsPosition)
    {
        var binaryTableHeader = new BinaryTableHeader();
        data.ReadBinaryStructAndAdvancePosition<Structured>(out var structured, ref position);
        binaryTableHeader.NameOffset = structured.NameOffset;
        binaryTableHeader.NameHash = structured.NameHash;
        binaryTableHeader.SchemaOffset = structured.SchemaOffset;
        binaryTableHeader.Type = structured.Type;
        binaryTableHeader.RowSize = structured.RowSize;
        binaryTableHeader.RowsOffset = structured.RowsOffset;
        binaryTableHeader.RowCount = structured.RowCount;
        if (binaryTableHeader.RowsOffset is BinaryUtilities.NullOffset)
            throw new InvalidOperationException("invalid rows offset");
        name = binaryTableHeader.NameOffset is not BinaryUtilities.NullOffset ? data.GetNullTerminatedString(position - sizeof(uint) * 7 + binaryTableHeader.NameOffset) : null;
        schemaPosition = binaryTableHeader.SchemaOffset is not BinaryUtilities.NullOffset ? position - sizeof(uint) * 5 + binaryTableHeader.SchemaOffset : null;
        type = (DataResourceType)binaryTableHeader.Type;
        rowsPosition = position - sizeof(uint) * 2 + binaryTableHeader.RowsOffset;
        return binaryTableHeader;
    }

    public uint NameOffset;
    public uint NameHash;
    public uint SchemaOffset;
    public uint Type;
    public uint RowSize;
    public uint RowsOffset;
    public uint RowCount;

    public bool IsExpectingHash =>
        true;

    public void Commit(in Memory<byte> memory)
    {
        var structured = new Structured
        {
            NameOffset = NameOffset,
            NameHash = NameHash,
            SchemaOffset = SchemaOffset,
            Type = Type,
            RowSize = RowSize,
            RowsOffset = RowsOffset,
            RowCount = RowCount
        };
        memory.WriteBinaryStruct(in structured);
    }

    public void SetRowsOffset(ArrayBufferWriter<byte> writer, in Range structRange) =>
        RowsOffset = writer.GetOffsetValueFromCurrentPosition(structRange, sizeof(uint) * 5);

    public void SetSchemaOffset(ArrayBufferWriter<byte> writer, in Range structRange) =>
        SchemaOffset = writer.GetOffsetValueFromCurrentPosition(structRange, sizeof(uint) * 2);

    public void SetStringOffset(ArrayBufferWriter<byte> writer, in Range structRange) =>
        throw new NotSupportedException();

    public void SetStringOffsetAndHash(ArrayBufferWriter<byte> writer, in Range structRange, uint hash)
    {
        NameOffset = writer.GetOffsetValueFromCurrentPosition(structRange, 0);
        NameHash = hash;
    }
}
