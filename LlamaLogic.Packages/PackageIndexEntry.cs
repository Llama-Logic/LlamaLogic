namespace LlamaLogic.Packages;

struct PackageIndexEntry(uint position, uint sizeCompressed, uint size, bool isCompressed)
{
    public bool IsCompressed = isCompressed;
    public uint Size = size;
    public uint SizeCompressed = sizeCompressed;
    public uint Position = position;

    internal void WriteIndexComponent(ref BinaryIndexEntry binaryIndexEntry)
    {
        binaryIndexEntry.SizeCompressed = SizeCompressed | 0x80000000;
        binaryIndexEntry.Size = Size;
        binaryIndexEntry.CompressionType = IsCompressed ? PackageResourceCompressionType.SmartSim : PackageResourceCompressionType.None;
        binaryIndexEntry.Committed = 1;
    }
}
