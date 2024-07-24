namespace LlamaLogic.Packages;

struct PackageIndexEntry(uint position, uint sizeCompressed, uint size, bool isCompressed)
{
    static readonly ReadOnlyMemory<byte> committed = BitConverter.GetBytes((ushort)1);

    public bool IsCompressed = isCompressed;
    public uint Size = size;
    public uint SizeCompressed = sizeCompressed;
    public uint Position = position;

    internal void WriteIndexComponent(ArrayBufferWriter<byte> index)
    {
        var sizeCompressed = SizeCompressed | 0x80000000;
        MemoryMarshal.Write(index.GetSpanAndAdvance(sizeof(uint)), ref sizeCompressed);
        MemoryMarshal.Write(index.GetSpanAndAdvance(sizeof(uint)), ref Size);
        var compression = IsCompressed ? PackageResourceCompressionType.SmartSim : PackageResourceCompressionType.None;
        MemoryMarshal.Write(index.GetSpanAndAdvance(sizeof(PackageResourceCompressionType)), ref compression);
        index.Write(committed.Span);
    }
}
