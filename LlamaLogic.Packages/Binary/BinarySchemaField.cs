namespace LlamaLogic.Packages.Binary;

class BinarySchemaField :
    IStringOffsetContainer
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Structured :
        IBinaryStruct
    {
        public uint NameOffset;
        public uint NameHash;
        public DataResourceType Type;
        public ushort Flags;
        public uint DataOffset;
        public uint SchemaOffset;
    }

    public static void CreateForSerialization(ArrayBufferWriter<byte> writer, out BinarySchemaField schemaField, out Memory<byte> schemaFieldMemory, out Range schemaFieldMemoryRange)
    {
        schemaField = new()
        {
            NameOffset = BinaryUtilities.NullOffset,
            NameHash = Fnv32.EmptyStringHash,
            SchemaOffset = BinaryUtilities.NullOffset
        };
        writer.ReserveMemoryForBinaryStruct<Structured>(out schemaFieldMemory, out schemaFieldMemoryRange);
    }

    public static BinarySchemaField Read(in ReadOnlySpan<byte> data, ref uint position, out string? name, out uint? schemaPosition)
    {
        var schemaField = new BinarySchemaField();
        data.ReadBinaryStructAndAdvancePosition<Structured>(out var structured, ref position);
        schemaField.NameOffset = structured.NameOffset;
        schemaField.NameHash = structured.NameHash;
        schemaField.Type = structured.Type;
        schemaField.Flags = structured.Flags;
        schemaField.DataOffset = structured.DataOffset;
        schemaField.SchemaOffset = structured.SchemaOffset;
        name = schemaField.NameOffset is not BinaryUtilities.NullOffset ? data.GetNullTerminatedString(position - (sizeof(uint) * 4 + sizeof(DataResourceType) + sizeof(ushort)) + schemaField.NameOffset) : null;
        schemaPosition = schemaField.SchemaOffset is not BinaryUtilities.NullOffset ? position - sizeof(uint) + schemaField.SchemaOffset : null;
        return schemaField;
    }

    public uint NameOffset;
    public uint NameHash;
    public DataResourceType Type;
    public ushort Flags;
    public uint DataOffset;
    public uint SchemaOffset;

    public bool IsExpectingHash =>
        true;

    public void Commit(in Memory<byte> memory)
    {
        var structured = new Structured
        {
            NameOffset = NameOffset,
            NameHash = NameHash,
            Type = Type,
            Flags = Flags,
            DataOffset = DataOffset,
            SchemaOffset = SchemaOffset
        };
        memory.WriteBinaryStruct(in structured);
    }

    public void SetSchemaOffset(ArrayBufferWriter<byte> writer, in Range structRange) =>
        SchemaOffset = writer.GetOffsetValueFromCurrentPosition(structRange, sizeof(uint) * 2 + sizeof(DataResourceType) + sizeof(ushort) + sizeof(uint));

    public void SetStringOffset(ArrayBufferWriter<byte> writer, in Range structRange) =>
        throw new NotSupportedException();

    public void SetStringOffsetAndHash(ArrayBufferWriter<byte> writer, in Range structRange, uint hash)
    {
        NameOffset = writer.GetOffsetValueFromCurrentPosition(structRange, 0);
        NameHash = hash;
    }
}
