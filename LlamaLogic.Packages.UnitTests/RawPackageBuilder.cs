namespace LlamaLogic.Packages.UnitTests;

static class RawPackageBuilder
{
    public static byte[] Build(IReadOnlyList<(ResourceKey key, byte[] content, bool hasExtendedCompressionType)> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return Build(entries.Select(entry => (entry.key, entry.content, (uint)entry.content.Length, entry.hasExtendedCompressionType ? (ushort?)0 : null)).ToList());
    }

    public static byte[] Build(IReadOnlyList<(ResourceKey key, byte[] content, uint sizeDecompressed, ushort? compressionType)> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(new byte[96]);
        var positions = new List<uint>();
        foreach (var (_, content, _, _) in entries)
        {
            positions.Add((uint)stream.Position);
            writer.Write(content);
        }
        var indexPosition = stream.Position;
        writer.Write(0U);
        for (var i = 0; i < entries.Count; ++i)
        {
            var (key, content, sizeDecompressed, compressionType) = entries[i];
            writer.Write((uint)key.Type);
            writer.Write(key.Group);
            writer.Write(key.InstanceEx);
            writer.Write(key.Instance);
            writer.Write(positions[i]);
            writer.Write((uint)content.Length | (compressionType is not null ? 0x80000000U : 0U));
            writer.Write(sizeDecompressed);
            if (compressionType is { } nonNullCompressionType)
            {
                writer.Write(nonNullCompressionType);
                writer.Write((ushort)1);
            }
        }
        var indexSize = stream.Position - indexPosition;
        stream.Position = 0;
        writer.Write("DBPF"u8);
        writer.Write(2U);
        writer.Write(1U);
        stream.Position = 36;
        writer.Write((uint)entries.Count);
        stream.Position = 44;
        writer.Write((uint)indexSize);
        stream.Position = 60;
        writer.Write(3U);
        writer.Write((ulong)indexPosition);
        writer.Flush();
        return stream.ToArray();
    }
}
