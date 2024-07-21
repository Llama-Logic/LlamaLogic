namespace LlamaLogic.Packages;

struct PackageIndexEntry(uint position, uint fileSize, uint memorySize, bool isCompressed)
{
    static readonly ReadOnlyMemory<byte> unused = BitConverter.GetBytes((ushort)1);

    public uint FileSize = fileSize;
    public bool IsCompressed = isCompressed;
    public uint MemorySize = memorySize;
    public uint Position = position;

    internal void WriteIndexComponent(ArrayBufferWriter<byte> index)
    {
        var fileSize = FileSize | 0x80000000;
        MemoryMarshal.Write(index.GetSpanAndAdvance(sizeof(uint)), ref fileSize);
        MemoryMarshal.Write(index.GetSpanAndAdvance(sizeof(uint)), ref MemorySize);
        var compression = IsCompressed ? PackageResourceCompressionType.SmartSim : PackageResourceCompressionType.None;
        MemoryMarshal.Write(index.GetSpanAndAdvance(sizeof(PackageResourceCompressionType)), ref compression);
        index.Write(unused.Span);
    }
}
