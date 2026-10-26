namespace LlamaLogic.Packages.Formats;

static class ImageEncoding
{
    public static ReadOnlyMemory<byte> EncodeGreyPng(byte[] pixels, int width, int height) =>
        EncodePng(pixels, width, height, 1, 0);

    static ReadOnlyMemory<byte> EncodePng(byte[] pixels, int width, int height, int bytesPerPixel, byte colorType)
    {
        var stride = width * bytesPerPixel;
        var filtered = new byte[(stride + 1) * height];
        var candidates = new byte[5 * stride];
        var noPriorRow = new byte[stride];
        for (var y = 0; y < height; ++y)
        {
            ReadOnlySpan<byte> row = pixels.AsSpan(y * stride, stride);
            ReadOnlySpan<byte> priorRow = y > 0 ? pixels.AsSpan((y - 1) * stride, stride) : noPriorRow;
            var bestFilter = 0;
            var bestScore = long.MaxValue;
            for (var filter = 0; filter < 5; ++filter)
            {
                var candidate = candidates.AsSpan(filter * stride, stride);
                var score = 0L;
                for (var i = 0; i < stride; ++i)
                {
                    var left = i >= bytesPerPixel ? row[i - bytesPerPixel] : 0;
                    var up = priorRow[i];
                    var upLeft = i >= bytesPerPixel ? priorRow[i - bytesPerPixel] : 0;
                    var prediction = filter switch
                    {
                        1 => left,
                        2 => up,
                        3 => (left + up) >> 1,
                        4 => PredictPaeth(left, up, upLeft),
                        _ => 0
                    };
                    var difference = (byte)(row[i] - prediction);
                    candidate[i] = difference;
                    score += Math.Abs((int)(sbyte)difference);
                }
                if (score < bestScore)
                {
                    bestFilter = filter;
                    bestScore = score;
                }
            }
            var filteredRowStart = y * (stride + 1);
            filtered[filteredRowStart] = (byte)bestFilter;
            candidates.AsSpan(bestFilter * stride, stride).CopyTo(filtered.AsSpan(filteredRowStart + 1, stride));
        }
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), height);
        header[8] = 8;
        header[9] = colorType;
        using var compressedStream = new ArrayBufferWriterOfByteStream();
        using (var compressionStream = new System.IO.Compression.ZLibStream(compressedStream, System.IO.Compression.CompressionLevel.Optimal, true))
            compressionStream.Write(filtered, 0, filtered.Length);
        using var stream = new ArrayBufferWriterOfByteStream();
        stream.Write(ImageDecoding.PngSignature.Span);
        WritePngChunk(stream, "IHDR"u8, header);
        WritePngChunk(stream, "IDAT"u8, compressedStream.WrittenMemory);
        WritePngChunk(stream, "IEND"u8, ReadOnlyMemory<byte>.Empty);
        return stream.WrittenMemory;
    }

    public static ReadOnlyMemory<byte> EncodeRgbaPng(byte[] pixels, int width, int height) =>
        EncodePng(pixels, width, height, 4, 6);

    public static ReadOnlyMemory<byte> EncodeRgbJpeg(byte[] pixels, int width, int height)
    {
        if (StbImageWrite.stbi__flip_vertically_on_write is not 0)
        {
            pixels = (byte[])pixels.Clone();
            ImageDecoding.FlipVertically(pixels, width * 3, height);
        }
        using var stream = new ArrayBufferWriterOfByteStream();
        new ImageWriter().WriteJpg(pixels, width, height, StbImageWriteSharp.ColorComponents.RedGreenBlue, stream, 100);
        return stream.WrittenMemory;
    }

    static int PredictPaeth(int left, int up, int upLeft)
    {
        var estimate = left + up - upLeft;
        var leftDistance = Math.Abs(estimate - left);
        var upDistance = Math.Abs(estimate - up);
        var upLeftDistance = Math.Abs(estimate - upLeft);
        if (leftDistance <= upDistance && leftDistance <= upLeftDistance)
            return left;
        return upDistance <= upLeftDistance ? up : upLeft;
    }

    static void WritePngChunk(Stream stream, ReadOnlySpan<byte> type, ReadOnlyMemory<byte> data)
    {
        Span<byte> field = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(field, data.Length);
        stream.Write(field);
        stream.Write(type);
        stream.Write(data.Span);
        var crc = new Crc32();
        foreach (var typeByte in type)
            crc.Update(typeByte);
        crc.Update(MemoryMarshal.TryGetArray(data, out var segment) ? segment : new ArraySegment<byte>(data.ToArray()));
        BinaryPrimitives.WriteUInt32BigEndian(field, (uint)crc.Value);
        stream.Write(field);
    }
}
