namespace LlamaLogic.Packages;

class ReadOnlyMemoryOfByteStream :
    Stream
{
    public ReadOnlyMemoryOfByteStream(ReadOnlyMemory<byte> readOnlyMemory) =>
        this.readOnlyMemory = readOnlyMemory;

    long position;
    readonly ReadOnlyMemory<byte> readOnlyMemory;

    public override bool CanRead =>
        true;

    public override bool CanSeek =>
        true;

    public override bool CanWrite =>
        false;

    public override long Length =>
        readOnlyMemory.Length;

    public override long Position
    {
        get => position;
        set
        {
            if (value < 0 || value > readOnlyMemory.Length)
                throw new ArgumentOutOfRangeException(nameof(value));
            position = value;
        }
    }

    public override void Flush()
    {
        // no op
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
#if IS_NET_6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(buffer);
#else
        if (buffer is null)
            throw new ArgumentNullException(nameof(buffer));
#endif
#if IS_NET_8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfLessThan(offset, 0);
#else
        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset));
#endif
#if IS_NET_8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0);
#else
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));
#endif
        if (buffer.Length - offset < count)
            throw new ArgumentException("invalid offset and length");
        if (position >= readOnlyMemory.Length)
            return 0;
        var positionAsInt = (int)position;
        var result = Math.Min(readOnlyMemory.Length - positionAsInt, count);
        readOnlyMemory.Span.Slice(positionAsInt, result).CopyTo(buffer.AsSpan(offset, result));
        position += result;
        return result;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => position + offset,
            SeekOrigin.End => readOnlyMemory.Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };
        if (newPosition < 0 || newPosition > readOnlyMemory.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));
        position = newPosition;
        return position;
    }

    public override void SetLength(long value) =>
        throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();
}
