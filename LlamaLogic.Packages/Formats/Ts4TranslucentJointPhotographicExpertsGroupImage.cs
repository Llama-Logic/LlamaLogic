namespace LlamaLogic.Packages.Formats;

/// <summary>
/// Provides methods for dealing with Maxis Translucent JPEGs
/// </summary>
public static class Ts4TranslucentJointPhotographicExpertsGroupImage
{
    static readonly ReadOnlyMemory<byte> jpegApp0Marker = new byte[] { 0xff, 0xe0 };

    /// <summary>
    /// Converts the data for a PNG the data for a Maxis Translucent JPEG
    /// </summary>
    /// <param name="pngData">The data for a PNG</param>
    public static ReadOnlyMemory<byte> ConvertPngToTranslucentJpeg(ReadOnlyMemory<byte> pngData)
    {
        ReadOnlyMemory<byte> jpegData, alphaData;
        using (var combinedStream = new ReadOnlyMemoryOfByteStream(pngData))
        using (var combined = Image.Load<Rgba32>(combinedStream))
        {
            using (var jpegImage = combined.Clone(c => c.BackgroundColor(Color.White)))
            {
                var jpegImageMetadata = jpegImage.Metadata;
                jpegImageMetadata.ExifProfile ??= new();
                jpegImageMetadata.ResolutionUnits = PixelResolutionUnit.AspectRatio;
                jpegImageMetadata.HorizontalResolution = 1;
                jpegImageMetadata.VerticalResolution = 1;
                using var jpegWriter = new ArrayBufferWriterOfByteStream();
                jpegImage.SaveAsJpeg(jpegWriter, new JpegEncoder { Quality = 100 });
                jpegData = jpegWriter.WrittenMemory;
            }
            using var alphaImage = new Image<L8>(combined.Width, combined.Height);
            for (var y = 0; y < combined.Height; ++y)
            {
                var combinedRow = combined.DangerousGetPixelRowMemory(y).Span;
                var pngRow = alphaImage.DangerousGetPixelRowMemory(y).Span;
                for (var x = 0; x < combined.Width; ++x)
                    pngRow[x] = new L8(combinedRow[x].A);
            }
            using var alphaWriter = new ArrayBufferWriterOfByteStream();
            alphaImage.SaveAsPng(alphaWriter, new PngEncoder
            {
                BitDepth = PngBitDepth.Bit8,
                ColorType = PngColorType.Grayscale,
                CompressionLevel = PngCompressionLevel.BestCompression,
                InterlaceMethod = PngInterlaceMode.None
            });
            alphaData = alphaWriter.WrittenMemory;
        }
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
    public static async Task<ReadOnlyMemory<byte>> ConvertPngToTranslucentJpegAsync(ReadOnlyMemory<byte> pngData, CancellationToken cancellationToken)
    {
        ReadOnlyMemory<byte> jpegData, alphaData;
        using (var combinedStream = new ReadOnlyMemoryOfByteStream(pngData))
        using (var combined = await Image.LoadAsync<Rgba32>(combinedStream, cancellationToken).ConfigureAwait(false))
        {
            using (var jpegImage = combined.Clone(c => c.BackgroundColor(Color.White)))
            {
                var jpegImageMetadata = jpegImage.Metadata;
                jpegImageMetadata.ExifProfile ??= new();
                jpegImageMetadata.ResolutionUnits = PixelResolutionUnit.AspectRatio;
                jpegImageMetadata.HorizontalResolution = 1;
                jpegImageMetadata.VerticalResolution = 1;
                using var jpegWriter = new ArrayBufferWriterOfByteStream();
                await jpegImage.SaveAsJpegAsync(jpegWriter, new JpegEncoder { Quality = 100 }, cancellationToken).ConfigureAwait(false);
                jpegData = jpegWriter.WrittenMemory;
            }
            using var alphaImage = new Image<L8>(combined.Width, combined.Height);
            for (var y = 0; y < combined.Height; ++y)
            {
                var combinedRow = combined.DangerousGetPixelRowMemory(y).Span;
                var pngRow = alphaImage.DangerousGetPixelRowMemory(y).Span;
                for (var x = 0; x < combined.Width; ++x)
                    pngRow[x] = new L8(combinedRow[x].A);
            }
            using var alphaWriter = new ArrayBufferWriterOfByteStream();
            await alphaImage.SaveAsPngAsync(alphaWriter, new PngEncoder
            {
                BitDepth = PngBitDepth.Bit8,
                ColorType = PngColorType.Grayscale,
                CompressionLevel = PngCompressionLevel.BestCompression,
                InterlaceMethod = PngInterlaceMode.None
            }, cancellationToken).ConfigureAwait(false);
            alphaData = alphaWriter.WrittenMemory;
        }
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
    /// Converts the data for a Maxis Translucent JPEG the data for a PNG
    /// </summary>
    /// <param name="translucentJpegData">The data for a Maxis Translucent JPEG</param>
    public static ReadOnlyMemory<byte> ConvertTranslucentJpegToPng(ReadOnlyMemory<byte> translucentJpegData)
    {
        using var contentStream = new ReadOnlyMemoryOfByteStream(translucentJpegData);
        if (Image.DetectFormat(contentStream) != JpegFormat.Instance)
            throw new FormatException($"{nameof(translucentJpegData)} is not a valid JPEG");
        ReadOnlyMemory<byte> alphaData = default;
        var contentScanningMemory = translucentJpegData;
        int scanIndex;
        while ((scanIndex = contentScanningMemory.Span.IndexOf(jpegApp0Marker.Span)) != -1)
        {
            var szSection = BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<ushort>(contentScanningMemory.Slice(scanIndex + 2, 2).Span));
            var section = contentScanningMemory.Slice(scanIndex + 4, szSection);
            if (section.Span[0..4].SequenceEqual("ALFA"u8))
            {
                alphaData = section[8..];
                break;
            }
            contentScanningMemory = contentScanningMemory[(scanIndex + szSection + 2)..];
        }
        if (alphaData.IsEmpty)
            throw new InvalidDataException("ALFA PNG APP0 marker not found in JPEG");
        using var jpegImage = Image.Load<Rgba32>(contentStream);
        using var pngStream = new ReadOnlyMemoryOfByteStream(alphaData);
        using var pngImage = Image.Load<L8>(pngStream);
        if (jpegImage.Size != pngImage.Size)
            throw new InvalidDataException("JPEG and PNG do not have identical dimensions");
        using var combined = new Image<Rgba32>(jpegImage.Width, jpegImage.Height);
        for (var y = 0; y < jpegImage.Height; ++y)
        {
            var jpegRow = jpegImage.DangerousGetPixelRowMemory(y).Span;
            var pngRow = pngImage.DangerousGetPixelRowMemory(y).Span;
            var combinedRow = combined.DangerousGetPixelRowMemory(y).Span;
            for (var x = 0; x < jpegImage.Width; ++x)
            {
                var jpegPixel = jpegRow[x];
                combinedRow[x] = new Rgba32(jpegPixel.R, jpegPixel.G, jpegPixel.B, pngRow[x].PackedValue);
            }
        }
        using var writer = new ArrayBufferWriterOfByteStream();
        combined.SaveAsPng(writer);
        return writer.WrittenMemory;
    }

    /// <summary>
    /// Converts the data for a Maxis Translucent JPEG the data for a PNG asynhronously
    /// </summary>
    /// <param name="translucentJpegData">The data for a Maxis Translucent JPEG</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    public static async Task<ReadOnlyMemory<byte>> ConvertTranslucentJpegToPngAsync(ReadOnlyMemory<byte> translucentJpegData, CancellationToken cancellationToken = default)
    {
        using var contentStream = new ReadOnlyMemoryOfByteStream(translucentJpegData);
        if (await Image.DetectFormatAsync(contentStream, cancellationToken).ConfigureAwait(false) != JpegFormat.Instance)
            throw new FormatException($"{nameof(translucentJpegData)} is not a valid JPEG");
        ReadOnlyMemory<byte> alphaData = default;
        var contentScanningMemory = translucentJpegData;
        int scanIndex;
        while ((scanIndex = contentScanningMemory.Span.IndexOf(jpegApp0Marker.Span)) != -1)
        {
            var szSection = BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<ushort>(contentScanningMemory.Slice(scanIndex + 2, 2).Span));
            var section = contentScanningMemory.Slice(scanIndex + 4, szSection);
            if (section.Span[0..4].SequenceEqual("ALFA"u8))
            {
                alphaData = section[8..];
                break;
            }
            contentScanningMemory = contentScanningMemory[(scanIndex + szSection + 2)..];
        }
        if (alphaData.IsEmpty)
            throw new InvalidDataException("ALFA PNG APP0 marker not found in JPEG");
        using var jpegImage = await Image.LoadAsync<Rgba32>(contentStream, cancellationToken).ConfigureAwait(false);
        using var pngStream = new ReadOnlyMemoryOfByteStream(alphaData);
        using var pngImage = await Image.LoadAsync<L8>(pngStream, cancellationToken).ConfigureAwait(false);
        if (jpegImage.Size != pngImage.Size)
            throw new InvalidDataException("JPEG and PNG do not have identical dimensions");
        using var combined = new Image<Rgba32>(jpegImage.Width, jpegImage.Height);
        for (var y = 0; y < jpegImage.Height; ++y)
        {
            var jpegRow = jpegImage.DangerousGetPixelRowMemory(y).Span;
            var pngRow = pngImage.DangerousGetPixelRowMemory(y).Span;
            var combinedRow = combined.DangerousGetPixelRowMemory(y).Span;
            for (var x = 0; x < jpegImage.Width; ++x)
            {
                var jpegPixel = jpegRow[x];
                combinedRow[x] = new Rgba32(jpegPixel.R, jpegPixel.G, jpegPixel.B, pngRow[x].PackedValue);
            }
        }
        using var writer = new ArrayBufferWriterOfByteStream();
        await combined.SaveAsPngAsync(writer, cancellationToken).ConfigureAwait(false);
        return writer.WrittenMemory;
    }
}
