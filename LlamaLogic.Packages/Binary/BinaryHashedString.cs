namespace LlamaLogic.Packages.Binary;

class BinaryHashedString :
    IStringOffsetContainer
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Structured :
        IBinaryStruct
    {
        public uint Offset;
        public uint Hash;
    }

    public static void CreateForSerialization(ArrayBufferWriter<byte> writer, out BinaryHashedString hashedString, out Memory<byte> hashedStringMemory, out Range hashedStringMemoryRange)
    {
        hashedString = new()
        {
            Offset = BinaryUtilities.NullOffset,
            Hash = Fnv32.EmptyStringHash
        };
        writer.ReserveMemoryForBinaryStruct<Structured>(out hashedStringMemory, out hashedStringMemoryRange);
    }

    public static BinaryHashedString Read(in ReadOnlySpan<byte> data, ref uint position, out string? str)
    {
        var binaryHashedString = new BinaryHashedString();
        data.ReadBinaryStructAndAdvancePosition<Structured>(out var structured, ref position);
        binaryHashedString.Offset = structured.Offset;
        binaryHashedString.Hash = structured.Hash;
        str = binaryHashedString.Offset is not BinaryUtilities.NullOffset ? data.GetNullTerminatedString(position - sizeof(uint)) : null;
        return binaryHashedString;
    }

    public uint Offset;
    public uint Hash;

    public bool IsExpectingHash =>
        true;

    public void Commit(in Memory<byte> memory)
    {
        var structured = new Structured
        {
            Offset = Offset,
            Hash = Hash
        };
        memory.WriteBinaryStruct(in structured);
    }

    public void SetStringOffset(ArrayBufferWriter<byte> writer, in Range structRange) =>
        throw new NotSupportedException();

    public void SetStringOffsetAndHash(ArrayBufferWriter<byte> writer, in Range structRange, uint hash)
    {
        Offset = writer.GetOffsetValueFromCurrentPosition(structRange, 0);
        Hash = hash;
    }
}
