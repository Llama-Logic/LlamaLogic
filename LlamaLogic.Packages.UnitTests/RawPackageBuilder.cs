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

    public static SparseStream BuildSparse(IReadOnlyList<(ResourceKey key, long position, int size, byte[] content)> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var indexPosition = entries.Max(entry => entry.position + entry.size);
        using var indexStream = new MemoryStream();
        using var indexWriter = new BinaryWriter(indexStream);
        indexWriter.Write(0U);
        foreach (var (key, position, size, _) in entries)
        {
            indexWriter.Write((uint)key.Type);
            indexWriter.Write(key.Group);
            indexWriter.Write(key.InstanceEx);
            indexWriter.Write(key.Instance);
            indexWriter.Write((uint)position);
            indexWriter.Write((uint)size | 0x80000000U);
            indexWriter.Write((uint)size);
            indexWriter.Write((ushort)0);
            indexWriter.Write((ushort)1);
        }
        indexWriter.Flush();
        var index = indexStream.ToArray();
        var header = new byte[96];
        "DBPF"u8.CopyTo(header);
        BitConverter.TryWriteBytes(header.AsSpan(4), 2U);
        BitConverter.TryWriteBytes(header.AsSpan(8), 1U);
        BitConverter.TryWriteBytes(header.AsSpan(36), (uint)entries.Count);
        BitConverter.TryWriteBytes(header.AsSpan(44), (uint)index.Length);
        BitConverter.TryWriteBytes(header.AsSpan(60), 3U);
        BitConverter.TryWriteBytes(header.AsSpan(64), (ulong)indexPosition);
        return new SparseStream(indexPosition + index.Length, [(0, header), .. entries.Select(entry => (entry.position, entry.content)), (indexPosition, index)]);
    }

    public static byte[] BuildVersion1(IReadOnlyList<(ResourceKey key, byte[] stored)> entries, uint indexMinorVersion)
    {
        ArgumentNullException.ThrowIfNull(entries);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(new byte[96]);
        var positions = new List<uint>();
        foreach (var (_, stored) in entries)
        {
            positions.Add((uint)stream.Position);
            writer.Write(stored);
        }
        var indexPosition = stream.Position;
        for (var i = 0; i < entries.Count; ++i)
        {
            var (key, stored) = entries[i];
            writer.Write((uint)key.Type);
            writer.Write(key.Group);
            writer.Write(key.Instance);
            if (indexMinorVersion >= 2)
                writer.Write(key.InstanceEx);
            writer.Write(positions[i]);
            writer.Write((uint)stored.Length);
        }
        var indexSize = stream.Position - indexPosition;
        stream.Position = 0;
        writer.Write("DBPF"u8);
        writer.Write(1U);
        writer.Write(1U);
        stream.Position = 32;
        writer.Write(7U);
        writer.Write((uint)entries.Count);
        writer.Write((uint)indexPosition);
        writer.Write((uint)indexSize);
        stream.Position = 60;
        writer.Write(indexMinorVersion);
        writer.Flush();
        return stream.ToArray();
    }

    public static byte[] BuildVersion1Compressed(byte[] content)
    {
        var compressed = DataBasePackedFile.InternalCompress(content, out var wasCompressed);
        if (!wasCompressed)
            throw new ArgumentException("content is not compressible", nameof(content));
        var stored = new byte[compressed.Length + 4];
        BitConverter.TryWriteBytes(stored.AsSpan(0, 4), (uint)stored.Length);
        compressed.Span.CopyTo(stored.AsSpan(4));
        return stored;
    }

    public static byte[] BuildVersion1Directory(IReadOnlyList<(ResourceKey key, int sizeDecompressed)> records, uint indexMinorVersion)
    {
        ArgumentNullException.ThrowIfNull(records);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        foreach (var (key, sizeDecompressed) in records)
        {
            writer.Write((uint)key.Type);
            writer.Write(key.Group);
            writer.Write(key.Instance);
            if (indexMinorVersion >= 2)
                writer.Write(key.InstanceEx);
            writer.Write((uint)sizeDecompressed);
        }
        writer.Flush();
        return stream.ToArray();
    }
}
