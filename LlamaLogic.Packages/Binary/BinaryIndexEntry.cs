namespace LlamaLogic.Packages.Binary;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct BinaryIndexEntry :
    IBinaryStruct
{
    public uint LowOrderInstance;
    public uint Position;
    public uint SizeCompressed;
    public uint Size;
    public PackageResourceCompressionType CompressionType;
    public ushort Committed;

    public void Read(PackageResourceType type, uint group, uint highOrderInstance, out PackageResourceKey packageResourceKey, out PackageIndexEntry packageIndexEntry)
    {
        packageResourceKey = new PackageResourceKey(type, group, ((ulong)highOrderInstance) << 32 | LowOrderInstance);
        SizeCompressed &= 0x7fffffff;
        packageIndexEntry = new PackageIndexEntry(Position, SizeCompressed, Size, CompressionType != PackageResourceCompressionType.None);
    }
}
