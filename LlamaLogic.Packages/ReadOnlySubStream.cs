namespace LlamaLogic.Packages;

sealed class ReadOnlySubStream :
    Stream
{
    public ReadOnlySubStream(Stream source, Range range)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanSeek)
            throw new ArgumentException("Source must be seekable", nameof(source));
        if (!source.CanRead)
            throw new ArgumentException("Source must be readable", nameof(source));
        (sourceOffset, length) = range.GetOffsetAndLength((int)source.Length);
        if (sourceOffset < 0)
            throw new ArgumentOutOfRangeException(nameof(range), "range lower bound cannot precede beginning of source");
        if (sourceOffset + length > source.Length)
            throw new ArgumentOutOfRangeException(nameof(range), "range upper bound cannot extend beyond ending of source");
        this.source = source;
        this.source.Seek(sourceOffset, SeekOrigin.Begin);
    }

    readonly long length;
    [SuppressMessage("Usage", "CA2213: Disposable fields should be disposed", Justification = "We didn't create this stream, so we're not disposing it. Sorry, code analyzer.")]
    readonly Stream source;
    readonly long sourceOffset;

    public override bool CanRead =>
        true;

    public override bool CanSeek =>
        true;

    public override bool CanWrite =>
        false;

    public override long Length =>
        length;

    public override long Position
    {
        get => source.Position - sourceOffset;
        set
        {
#if IS_NET_8_0_OR_GREATER
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(value));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, length, nameof(value));
#else
            if (value < 0 || value > length)
                throw new ArgumentOutOfRangeException(nameof(value));
#endif
            source.Position = sourceOffset + value;
        }
    }

    public override void Flush()
    {
        // no op
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
#if IS_NET_8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfLessThan(offset, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0);
#else
        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset));
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));
#endif
        if (buffer.Length - offset < count)
            throw new ArgumentException("invalid offset and count");
        if (Position >= length)
            return 0;
        return source.Read(buffer, offset, Math.Min((int)length - (int)Position, count));
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };
#if IS_NET_8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfNegative(newPosition, nameof(offset));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(newPosition, length, nameof(offset));
#else
        if (newPosition < 0 || newPosition > length)
            throw new ArgumentOutOfRangeException(nameof(offset));
#endif
        source.Position = sourceOffset + newPosition;
        return newPosition;
    }

    public override void SetLength(long value) =>
        throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();
}
