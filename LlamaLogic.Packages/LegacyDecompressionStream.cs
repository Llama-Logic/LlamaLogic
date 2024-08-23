namespace LlamaLogic.Packages;

sealed class LegacyDecompressionStream :
    Stream
{
    public LegacyDecompressionStream(Stream compressedStream) =>
        this.compressedStream = compressedStream;

    readonly Stream compressedStream;
    ReadOnlyMemoryOfByteStream? decompressedStream;

    public override bool CanRead =>
        true;

    public override bool CanSeek =>
        true;

    public override bool CanWrite =>
        false;

    public override long Length
    {
        get
        {
            Decompress();
            return decompressedStream.Length;
        }
    }

    public override long Position
    {
        get => decompressedStream?.Position ?? 0;
        set
        {
            Decompress();
            decompressedStream.Position = value;
        }
    }

    static void CopyCompressedText(byte[] data, int numToCopy, ref int position, int copyOffest)
    {
        var currentPosition = position;
        for (var i = 0; i < numToCopy; i++, position++)
            data[position] = data[currentPosition - copyOffest + i];
    }

    [MemberNotNull(nameof(decompressedStream))]
    void Decompress()
    {
        if (decompressedStream is not null)
            return;
        Span<byte> decompressedSizeTypeSpan = stackalloc byte[1];
        compressedStream.Read(decompressedSizeTypeSpan);
        var decompressedSizeByteLength = decompressedSizeTypeSpan[0] != 0x80 ? 4 : 3;
        compressedStream.Seek(1, SeekOrigin.Current);
        Span<byte> decompressedSizeSpan = stackalloc byte[4];
        var decompressionSizeReadingSlice = decompressedSizeSpan[..decompressedSizeByteLength];
        compressedStream.Read(decompressionSizeReadingSlice);
        decompressionSizeReadingSlice.Reverse(); // the one time Maxis ever used big endian -- seriously, WTF?
        var decompressedSize = MemoryMarshal.Read<int>(decompressedSizeSpan);
        Memory<byte> decompressedMemory = new byte[decompressedSize];
        var position = 0;
        Span<byte> firstByteSpan = stackalloc byte[1];
        Span<byte> remainingBytesSpan = stackalloc byte[3];
        while (position < decompressedSize)
        {
            var readSize = 0;
            int? copySize = null;
            int? copyOffset = null;
            compressedStream.Read(firstByteSpan);
            var firstByte = firstByteSpan[0];
            if (firstByte <= 0x7f)
            {
                compressedStream.Read(remainingBytesSpan[..1]);
                var secondByte = remainingBytesSpan[0];
                readSize = firstByte & 0x03;
                copySize = ((firstByte & 0x1c) >> 2) + 3;
                copyOffset = ((firstByte & 0x60) << 3) + secondByte + 1;
            }
            else if (firstByte is > 0x7f and <= 0xbf)
            {
                compressedStream.Read(remainingBytesSpan[..2]);
                var secondByte = remainingBytesSpan[0];
                var thirdByte = remainingBytesSpan[1];
                readSize = ((secondByte & 0xc0) >> 6) & 0x03;
                copySize = (firstByte & 0x3f) + 4;
                copyOffset = ((secondByte & 0x3f) << 8) + thirdByte + 1;
            }
            else if (firstByte is > 0xbf and <= 0xdf)
            {
                compressedStream.Read(remainingBytesSpan);
                var secondByte = remainingBytesSpan[0];
                var thirdByte = remainingBytesSpan[1];
                var fourthByte = remainingBytesSpan[2];
                readSize = firstByte & 0x03;
                copySize = ((firstByte & 0x0c) << 6) + fourthByte + 5;
                copyOffset = ((firstByte & 0x10) << 12) + (secondByte << 8) + thirdByte + 1;
            }
            else if (firstByte is > 0xdf and <= 0xfb)
                readSize = ((firstByte & 0x1f) << 2) + 4;
            else
                readSize = firstByte & 0x03;
            compressedStream.Read(decompressedMemory.Span[position..(position += readSize)]);
            if (copySize is { } nonNullCopySize && copyOffset is { } nonNullCopyOffset)
                decompressedMemory[(position - nonNullCopyOffset)..(position - nonNullCopyOffset + nonNullCopySize)].CopyTo(decompressedMemory[position..(position += nonNullCopySize)]);
        }
        decompressedStream = new(decompressedMemory);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            decompressedStream?.Dispose();
            compressedStream.Dispose();
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync().ConfigureAwait(false);
        if (decompressedStream is not null)
            await decompressedStream.DisposeAsync().ConfigureAwait(false);
        await compressedStream.DisposeAsync().ConfigureAwait(false);
    }

    public override void Flush()
    {
        // no op
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        Decompress();
        return decompressedStream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        Decompress();
        return decompressedStream.Seek(offset, origin);
    }

    public override void SetLength(long value) =>
        throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();
}
