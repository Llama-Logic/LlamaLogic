namespace LlamaLogic.Packages;

/// <summary>
/// Represents a read-only, seekable <see cref="Stream"/> over a <see cref="ReadOnlyMemory{T}"/> of bytes (🔓)
/// </summary>
public sealed class ReadOnlyMemoryOfByteStream :
    Stream
{
    /// <summary>
    /// Initializes a new <see cref="ReadOnlyMemoryOfByteStream"/> over the specified <paramref name="readOnlyMemoryOfBytes"/>
    /// </summary>
    public ReadOnlyMemoryOfByteStream(ReadOnlyMemory<byte> readOnlyMemoryOfBytes) =>
        Memory = readOnlyMemoryOfBytes;

    long position;

    /// <inheritdoc/>
    public override bool CanRead =>
        true;

    /// <inheritdoc/>
    public override bool CanSeek =>
        true;

    /// <inheritdoc/>
    public override bool CanWrite =>
        false;

    /// <inheritdoc/>
    public override long Length =>
        Memory.Length;

    /// <summary>
    /// Gets the bytes the stream reads
    /// </summary>
    public ReadOnlyMemory<byte> Memory { get; }

    /// <inheritdoc/>
    public override long Position
    {
        get => position;
        set
        {
#if IS_NET_8_0_OR_GREATER
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(value));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, Memory.Length, nameof(value));
#else
            if (value < 0 || value > Memory.Length)
                throw new ArgumentOutOfRangeException(nameof(value));
#endif
            position = value;
        }
    }

    /// <inheritdoc/>
    public override void Flush()
    {
        // no op
    }

    /// <inheritdoc/>
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
            throw new ArgumentException("invalid offset and length");
        if (position >= Memory.Length)
            return 0;
        var positionAsInt = (int)position;
        var result = Math.Min(Memory.Length - positionAsInt, count);
        Memory.Span.Slice(positionAsInt, result).CopyTo(buffer.AsSpan(offset, result));
        position += result;
        return result;
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
    {
        var newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => position + offset,
            SeekOrigin.End => Memory.Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };
#if IS_NET_8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfNegative(newPosition, nameof(offset));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(newPosition, Memory.Length, nameof(offset));
#else
        if (newPosition < 0 || newPosition > Memory.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));
#endif
        position = newPosition;
        return position;
    }

    /// <inheritdoc/>
    public override void SetLength(long value) =>
        throw new NotSupportedException();

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();
}
