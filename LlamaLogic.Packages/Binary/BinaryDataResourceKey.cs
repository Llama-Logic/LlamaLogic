namespace LlamaLogic.Packages.Binary;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct BinaryDataResourceKey :
    IBinaryStruct
{
    public static PackageResourceKey Read(in ReadOnlySpan<byte> data, ref uint position)
    {
        data.ReadBinaryStructAndAdvancePosition<BinaryDataResourceKey>(out var binaryResourceKey, ref position);
        return new PackageResourceKey(binaryResourceKey.Type, binaryResourceKey.Group, binaryResourceKey.Instance);
    }

    public static void Write(in PackageResourceKey resourceKey, ArrayBufferWriter<byte> writer)
    {
        var binaryResourceKey = new BinaryDataResourceKey
        {
            Type = resourceKey.Type,
            Group = resourceKey.Group,
            Instance = resourceKey.Instance
        };
        writer.WriteBinaryStruct(in binaryResourceKey);
    }

    public ulong Instance;
    public PackageResourceType Type;
    public uint Group;
}
