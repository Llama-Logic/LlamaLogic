namespace LlamaLogic.TuningArchive;

readonly record struct BlobHash(ulong High, ulong Low)
{
    public static BlobHash Compute(ReadOnlySpan<byte> content)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(content, hash);
        return FromBytes(hash);
    }

    public static BlobHash FromBytes(ReadOnlySpan<byte> hash) =>
        new(BinaryPrimitives.ReadUInt64BigEndian(hash), BinaryPrimitives.ReadUInt64BigEndian(hash[8..]));

    public byte[] ToBytes()
    {
        var bytes = new byte[16];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, High);
        BinaryPrimitives.WriteUInt64BigEndian(bytes.AsSpan(8), Low);
        return bytes;
    }
}