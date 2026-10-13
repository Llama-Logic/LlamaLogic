namespace LlamaLogic.Packages.UnitTests;

sealed class SparseStream(long length, IEnumerable<(long offset, byte[] data)> segments) :
    Stream
{
    static readonly byte[] zeros = new byte[81920];

    long length = length;
    long position;
    readonly List<(long offset, byte[] data)> segments = [.. segments];

    public SparseStream() :
        this(0, [])
    {
    }

    public override bool CanRead =>
        true;

    public override bool CanSeek =>
        true;

    public override bool CanWrite =>
        true;

    public override long Length =>
        length;

    public override long Position
    {
        get => position;
        set => position = value;
    }

    public override void Flush()
    {
    }

    static bool IsZero(ReadOnlySpan<byte> span)
    {
        for (var start = 0; start < span.Length; start += zeros.Length)
        {
            var chunk = span[start..Math.Min(span.Length, start + zeros.Length)];
            if (!chunk.SequenceEqual(zeros.AsSpan(0, chunk.Length)))
                return false;
        }
        return true;
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        var count = (int)Math.Max(0, Math.Min(buffer.Length, length - position));
        var destination = buffer[..count];
        destination.Clear();
        foreach (var (offset, data) in segments)
        {
            var start = Math.Max(offset, position);
            var end = Math.Min(offset + data.Length, position + count);
            if (start < end)
                data.AsSpan((int)(start - offset), (int)(end - start)).CopyTo(destination[(int)(start - position)..]);
        }
        position += count;
        return count;
    }

    public override long Seek(long offset, SeekOrigin origin) =>
        position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => position + offset,
            SeekOrigin.End => length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

    public override void SetLength(long value) =>
        length = value;

    public override void Write(byte[] buffer, int offset, int count) =>
        Write(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        var end = position + buffer.Length;
        if (!IsZero(buffer) || segments.Any(segment => segment.offset < end && position < segment.offset + segment.data.Length))
            segments.Add((position, buffer.ToArray()));
        position = end;
        length = Math.Max(length, position);
    }
}
