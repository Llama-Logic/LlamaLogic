namespace LlamaLogic.Packages.Formats;

/// <summary>
/// Provides methods for dealing with Maxis Translucent JPEGs
/// </summary>
public static class Ts4TranslucentJointPhotographicExpertsGroupImage
{
    const int maximumAlphaDataLength = ushort.MaxValue - 10;

    static readonly ReadOnlyMemory<byte> jpegApp0Marker = new byte[] { 0xff, 0xe0 };

    /// <summary>
    /// Converts the data for a PNG the data for a Maxis Translucent JPEG
    /// </summary>
    /// <param name="pngData">The data for a PNG</param>
    /// <exception cref="InvalidDataException"><paramref name="pngData"/> is not a PNG, could not be decoded, or has more than 67,108,864 (8,192 by 8,192) pixels, or its alpha channel compresses to more than 65,525 bytes</exception>
    public static ReadOnlyMemory<byte> ConvertPngToTranslucentJpeg(ReadOnlyMemory<byte> pngData)
    {
        var combined = ImageDecoding.DecodePngAsRgba(pngData, out var width, out var height);
        var pixelCount = width * height;
        var jpegPixels = new byte[pixelCount * 3];
        var alphaPixels = new byte[pixelCount];
        for (var i = 0; i < pixelCount; ++i)
        {
            var alpha = combined[(i * 4) + 3];
            var white = 255 * (255 - alpha);
            for (var channel = 0; channel < 3; ++channel)
                jpegPixels[(i * 3) + channel] = (byte)(((combined[(i * 4) + channel] * alpha) + white + 127) / 255);
            alphaPixels[i] = alpha;
        }
        var jpegData = ImageEncoding.EncodeRgbJpeg(jpegPixels, width, height);
        var alphaData = ImageEncoding.EncodeGreyPng(alphaPixels, width, height);
        if (alphaData.Length > maximumAlphaDataLength)
            throw new InvalidDataException($"The alpha channel compresses to {alphaData.Length} bytes; a translucent JPEG can hold at most {maximumAlphaDataLength}");
        var writer = new ArrayBufferWriter<byte>();
        var firstMarkerSzSection = MemoryMarshal.Read<ushort>(jpegData.Span[4..6]);
        firstMarkerSzSection = BinaryPrimitives.ReverseEndianness(firstMarkerSzSection);
        var insertionIndex = 4 + firstMarkerSzSection;
        writer.Write(jpegData.Span[..insertionIndex]);
        writer.Write(jpegApp0Marker.Span);
        var szSection = (ushort)(alphaData.Length + 10);
        szSection = BinaryPrimitives.ReverseEndianness(szSection);
        writer.Write(ref szSection);
        writer.Write("ALFA"u8);
        var alphaLength = (uint)alphaData.Length;
        alphaLength = BinaryPrimitives.ReverseEndianness(alphaLength);
        writer.Write(ref alphaLength);
        writer.Write(alphaData.Span);
        writer.Write(jpegData.Span[insertionIndex..]);
        return writer.WrittenMemory;
    }

    /// <summary>
    /// Converts the data for a PNG the data for a Maxis Translucent JPEG asynhronously
    /// </summary>
    /// <param name="pngData">The data for a PNG</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    /// <exception cref="InvalidDataException"><paramref name="pngData"/> is not a PNG, could not be decoded, or has more than 67,108,864 (8,192 by 8,192) pixels, or its alpha channel compresses to more than 65,525 bytes</exception>
    public static Task<ReadOnlyMemory<byte>> ConvertPngToTranslucentJpegAsync(ReadOnlyMemory<byte> pngData, CancellationToken cancellationToken) =>
        Task.Run(() => ConvertPngToTranslucentJpeg(pngData), cancellationToken);

    /// <summary>
    /// Converts the data for a Maxis Translucent JPEG the data for a PNG
    /// </summary>
    /// <param name="translucentJpegData">The data for a Maxis Translucent JPEG</param>
    /// <exception cref="FormatException"><paramref name="translucentJpegData"/> is not a JPEG</exception>
    /// <exception cref="InvalidDataException">The ALFA PNG APP0 marker is missing or malformed, the JPEG or the PNG could not be decoded or has more than 67,108,864 (8,192 by 8,192) pixels, or their dimensions differ</exception>
    public static ReadOnlyMemory<byte> ConvertTranslucentJpegToPng(ReadOnlyMemory<byte> translucentJpegData)
    {
        if (!ImageDecoding.IsJpeg(translucentJpegData.Span))
            throw new FormatException($"{nameof(translucentJpegData)} is not a valid JPEG");
        ReadOnlyMemory<byte> alphaData = default;
        var contentScanningMemory = translucentJpegData;
        int scanIndex;
        while ((scanIndex = contentScanningMemory.Span.IndexOf(jpegApp0Marker.Span)) != -1)
        {
            var szSection = BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<ushort>(contentScanningMemory.Slice(scanIndex + 2, 2).Span));
            var section = contentScanningMemory.Slice(scanIndex + 4, szSection - 2);
            if (section.Length >= 8 && section.Span[0..4].SequenceEqual("ALFA"u8))
            {
                var alphaLength = BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<uint>(section.Span[4..8]));
                if (alphaLength > section.Length - 8)
                    throw new InvalidDataException("ALFA PNG APP0 marker declares more data than its section contains");
                alphaData = section.Slice(8, (int)alphaLength);
                break;
            }
            contentScanningMemory = contentScanningMemory[(scanIndex + szSection + 2)..];
        }
        if (alphaData.IsEmpty)
            throw new InvalidDataException("ALFA PNG APP0 marker not found in JPEG");
        var alphaPixels = ImageDecoding.DecodePngAsLuminance(alphaData, out var alphaWidth, out var alphaHeight);
        var jpegPixels = ImageDecoding.DecodeJpegAsRgb(translucentJpegData, out var width, out var height);
        if (width != alphaWidth || height != alphaHeight)
            throw new InvalidDataException("JPEG and PNG do not have identical dimensions");
        var pixelCount = width * height;
        var combined = new byte[pixelCount * 4];
        for (var i = 0; i < pixelCount; ++i)
        {
            combined[i * 4] = jpegPixels[i * 3];
            combined[(i * 4) + 1] = jpegPixels[(i * 3) + 1];
            combined[(i * 4) + 2] = jpegPixels[(i * 3) + 2];
            combined[(i * 4) + 3] = alphaPixels[i];
        }
        return ImageEncoding.EncodeRgbaPng(combined, width, height);
    }

    /// <summary>
    /// Converts the data for a Maxis Translucent JPEG the data for a PNG asynhronously
    /// </summary>
    /// <param name="translucentJpegData">The data for a Maxis Translucent JPEG</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    /// <exception cref="FormatException"><paramref name="translucentJpegData"/> is not a JPEG</exception>
    /// <exception cref="InvalidDataException">The ALFA PNG APP0 marker is missing or malformed, the JPEG or the PNG could not be decoded or has more than 67,108,864 (8,192 by 8,192) pixels, or their dimensions differ</exception>
    public static Task<ReadOnlyMemory<byte>> ConvertTranslucentJpegToPngAsync(ReadOnlyMemory<byte> translucentJpegData, CancellationToken cancellationToken = default) =>
        Task.Run(() => ConvertTranslucentJpegToPng(translucentJpegData), cancellationToken);
}
