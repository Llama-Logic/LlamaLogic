namespace LlamaLogic.Packages.Binary;

class BinarySchemaHeader :
    IStringOffsetContainer
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Structured :
        IBinaryStruct
    {
        public uint NameOffset;
        public uint NameHash;
        public uint SchemaHash; // "nobody knows" — Lot 51
        public uint Size;
        public uint FieldsOffset;
        public uint FieldCount;
    }

    public static void CreateForSerialization(ArrayBufferWriter<byte> writer, out BinarySchemaHeader schemaHeader, out Memory<byte> schemaHeaderMemory, out Range schemaHeaderMemoryRange)
    {
        schemaHeader = new()
        {
            NameOffset = BinaryUtilities.NullOffset,
            NameHash = Fnv32.EmptyStringHash,
            FieldsOffset = BinaryUtilities.NullOffset
        };
        writer.ReserveMemoryForBinaryStruct<Structured>(out schemaHeaderMemory, out schemaHeaderMemoryRange);
    }

    public static BinarySchemaHeader Read(in ReadOnlySpan<byte> data, ref uint position, out string? name, out uint? fieldsPosition)
    {
        var schemaHeader = new BinarySchemaHeader();
        data.ReadBinaryStructAndAdvancePosition<Structured>(out var structured, ref position);
        schemaHeader.NameOffset = structured.NameOffset;
        schemaHeader.NameHash = structured.NameHash;
        schemaHeader.SchemaHash = structured.SchemaHash;
        schemaHeader.Size = structured.Size;
        schemaHeader.FieldsOffset = structured.FieldsOffset;
        schemaHeader.FieldCount = structured.FieldCount;
        name = schemaHeader.NameOffset is not BinaryUtilities.NullOffset ? data.GetNullTerminatedString(position - sizeof(uint) * 6 + schemaHeader.NameOffset) : null;
        fieldsPosition = schemaHeader.FieldsOffset is not BinaryUtilities.NullOffset ? position - sizeof(uint) * 2 + schemaHeader.FieldsOffset : null;
        return schemaHeader;
    }

    public uint NameOffset;
    public uint NameHash;
    public uint SchemaHash; // "nobody knows" — Lot 51
    public uint Size;
    public uint FieldsOffset;
    public uint FieldCount;

    public bool IsExpectingHash =>
        true;

    public void Commit(in Memory<byte> memory)
    {
        var structured = new Structured
        {
            NameOffset = NameOffset,
            NameHash = NameHash,
            SchemaHash = SchemaHash,
            Size = Size,
            FieldsOffset = FieldsOffset,
            FieldCount = FieldCount
        };
        memory.WriteBinaryStruct(in structured);
    }

    public void SetFieldsOffset(ArrayBufferWriter<byte> writer, in Range structRange) =>
        FieldsOffset = writer.GetOffsetValueFromCurrentPosition(structRange, sizeof(uint) * 4);

    public void SetStringOffset(ArrayBufferWriter<byte> writer, in Range structRange) =>
        throw new NotSupportedException();

    public void SetStringOffsetAndHash(ArrayBufferWriter<byte> writer, in Range structRange, uint hash)
    {
        NameOffset = writer.GetOffsetValueFromCurrentPosition(structRange, 0);
        NameHash = hash;
    }
}
