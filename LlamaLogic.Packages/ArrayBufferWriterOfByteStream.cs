namespace LlamaLogic.Packages;

/// <summary>
/// Represents a write-only <see cref="Stream"/> that collects what is written to it in memory (🔓)
/// </summary>
/// <param name="initialCapacity">The number of bytes to allocate initially, or <see langword="null"/> for the default</param>
/// <remarks>
/// <see cref="SetLength(long)"/> changes the length the stream reports: <see cref="WrittenMemory"/> and <see cref="WrittenSpan"/> are then truncated to it, or extended to it with zeros.
/// </remarks>
public sealed class ArrayBufferWriterOfByteStream(int? initialCapacity = null) :
    Stream
{
    readonly ArrayBufferWriter<byte> writer = initialCapacity is { } specifiedInitialCapacity ? new(specifiedInitialCapacity) : new();
    long? setLength;

    /// <inheritdoc/>
    public override bool CanRead =>
        false;

    /// <inheritdoc/>
    public override bool CanSeek =>
        false;

    /// <inheritdoc/>
    public override bool CanWrite =>
        true;

    /// <inheritdoc/>
    public override long Length =>
        setLength is { } nonNullSetLength ? nonNullSetLength : writer.WrittenCount;

    /// <inheritdoc/>
    public override long Position
    {
        get => setLength is { } nonNullSetLength && nonNullSetLength < writer.WrittenCount ? nonNullSetLength : writer.WrittenCount;
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Gets the bytes written to the stream
    /// </summary>
    public ReadOnlyMemory<byte> WrittenMemory
    {
        get
        {
            if (setLength is not { } nonNullSetLength)
                return writer.WrittenMemory;
            if (nonNullSetLength < writer.WrittenCount)
                return writer.WrittenMemory[..(int)nonNullSetLength];
            Memory<byte> result = new byte[nonNullSetLength];
            writer.WrittenMemory.CopyTo(result);
            return result;
        }
    }

    /// <summary>
    /// Gets the bytes written to the stream
    /// </summary>
    public ReadOnlySpan<byte> WrittenSpan
    {
        get
        {
            if (setLength is not { } nonNullSetLength)
                return writer.WrittenSpan;
            if (nonNullSetLength < writer.WrittenCount)
                return writer.WrittenSpan[..(int)nonNullSetLength];
            Span<byte> result = new byte[nonNullSetLength];
            writer.WrittenSpan.CopyTo(result);
            return result;
        }
    }

    /// <inheritdoc/>
    public override void Flush()
    {
        // no op
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException();

    /// <inheritdoc/>
    public override void SetLength(long value) =>
        setLength = value;

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
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
        writer.Write(buffer.AsSpan(offset, count));
    }
}
