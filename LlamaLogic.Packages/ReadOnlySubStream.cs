namespace LlamaLogic.Packages;

class ReadOnlySubStream :
    Stream
{
    public ReadOnlySubStream(Stream source, Range range)
    {
#if IS_NET_6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(source);
#else
        if (source is null)
            throw new ArgumentNullException(nameof(source));
#endif
        (sourceOffset, length) = range.GetOffsetAndLength((int)source.Length);
        if (sourceOffset < 0)
            throw new ArgumentOutOfRangeException(nameof(range), "range lower bound cannot precede beginning of source");
        if (sourceOffset + length > source.Length)
            throw new ArgumentOutOfRangeException(nameof(range), "range upper bound cannot extend beyond ending of source");
        this.source = source;
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
            if (value < 0 || value > length)
                throw new ArgumentOutOfRangeException(nameof(value));
            source.Position = sourceOffset + value;
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
        if (newPosition < 0 || newPosition > length)
            throw new ArgumentOutOfRangeException(nameof(offset));
        source.Position = sourceOffset + newPosition;
        return newPosition;
    }

    public override void SetLength(long value) =>
        throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();
}
