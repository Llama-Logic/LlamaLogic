namespace LlamaLogic.Packages.Binary;

class BinaryString :
    IStringOffsetContainer
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Structured :
        IBinaryStruct
    {
        public uint Offset;
    }

    public static void CreateForSerialization(ArrayBufferWriter<byte> writer, out BinaryString str, out Memory<byte> stringMemory, out Range stringMemoryRange)
    {
        str = new()
        {
            Offset = BinaryUtilities.NullOffset
        };
        writer.ReserveMemoryForBinaryStruct<Structured>(out stringMemory, out stringMemoryRange);
    }

    public static BinaryString Read(in ReadOnlySpan<byte> data, ref uint position, out string? str)
    {
        var binaryString = new BinaryString();
        data.ReadBinaryStructAndAdvancePosition<Structured>(out var structured, ref position);
        binaryString.Offset = structured.Offset;
        str = binaryString.Offset is not BinaryUtilities.NullOffset ? data.GetNullTerminatedString(position - sizeof(uint)) : null;
        return binaryString;
    }

    public uint Offset;

    public bool IsExpectingHash =>
        false;

    public void Commit(in Memory<byte> memory)
    {
        var structured = new Structured
        {
            Offset = Offset
        };
        memory.WriteBinaryStruct(in structured);
    }

    public void SetStringOffset(ArrayBufferWriter<byte> writer, in Range structRange) =>
        Offset = writer.GetOffsetValueFromCurrentPosition(structRange, 0);

    public void SetStringOffsetAndHash(ArrayBufferWriter<byte> writer, in Range structRange, uint hash) =>
        throw new NotSupportedException();
}
