namespace LlamaLogic.Packages.Formats;

static class ImageDecoding
{
    public const int MaximumPixelCount = 8192 * 8192;

    static bool IsFlippingVerticallyOnLoad =>
        (StbImage.stbi__vertically_flip_on_load_set is not 0 ? StbImage.stbi__vertically_flip_on_load_local : StbImage.stbi__vertically_flip_on_load_global) is not 0;

    public static ReadOnlyMemory<byte> PngSignature { get; } = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };

    static byte[] Decode(ReadOnlyMemory<byte> data, StbImageSharp.ColorComponents components, out int width, out int height)
    {
        ImageInfo? info;
        ImageResult result;
        try
        {
            using (var infoStream = new ReadOnlyMemoryOfByteStream(data))
                info = ImageInfo.FromStream(infoStream);
            if (info is not { } nonNullInfo)
                throw new InvalidDataException("The image header could not be read");
            RequireSupportedSize(nonNullInfo.Width, nonNullInfo.Height);
            using var decodeStream = new ReadOnlyMemoryOfByteStream(data);
            result = ImageResult.FromStream(decodeStream, components);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidDataException("The image could not be decoded", ex);
        }
        if (IsFlippingVerticallyOnLoad)
            FlipVertically(result.Data, result.Width * (int)components, result.Height);
        width = result.Width;
        height = result.Height;
        return result.Data;
    }

    public static byte[] DecodeJpegAsRgb(ReadOnlyMemory<byte> jpegData, out int width, out int height)
    {
        if (!IsJpeg(jpegData.Span))
            throw new InvalidDataException("The data is not a JPEG");
        return Decode(jpegData, StbImageSharp.ColorComponents.RedGreenBlue, out width, out height);
    }

    public static byte[] DecodePngAsLuminance(ReadOnlyMemory<byte> pngData, out int width, out int height)
    {
        if (!IsPng(pngData.Span))
            throw new InvalidDataException("The data is not a PNG");
        var rgb = Decode(pngData, StbImageSharp.ColorComponents.RedGreenBlue, out width, out height);
        var luminance = new byte[width * height];
        for (var i = 0; i < luminance.Length; ++i)
            luminance[i] = (byte)((rgb[i * 3] * .2126F) + (rgb[(i * 3) + 1] * .7152F) + (rgb[(i * 3) + 2] * .0722F) + 0.5F);
        return luminance;
    }

    public static byte[] DecodePngAsRgba(ReadOnlyMemory<byte> pngData, out int width, out int height)
    {
        if (!IsPng(pngData.Span))
            throw new InvalidDataException("The data is not a PNG");
        return Decode(pngData, StbImageSharp.ColorComponents.RedGreenBlueAlpha, out width, out height);
    }

    public static void FlipVertically(byte[] pixels, int stride, int height)
    {
        var row = ArrayPool<byte>.Shared.Rent(stride);
        try
        {
            var rowSpan = row.AsSpan(0, stride);
            for (int top = 0, bottom = height - 1; top < bottom; ++top, --bottom)
            {
                var topSpan = pixels.AsSpan(top * stride, stride);
                var bottomSpan = pixels.AsSpan(bottom * stride, stride);
                topSpan.CopyTo(rowSpan);
                bottomSpan.CopyTo(topSpan);
                rowSpan.CopyTo(bottomSpan);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(row);
        }
    }

    public static bool IsJpeg(ReadOnlySpan<byte> data) =>
        data.Length >= 3 && data[0] is 0xff && data[1] is 0xd8 && data[2] is 0xff;

    public static bool IsPng(ReadOnlySpan<byte> data) =>
        data.Length >= 16 && data[..8].SequenceEqual(PngSignature.Span) && data[12..16].SequenceEqual("IHDR"u8);

    public static void RequireSupportedSize(long width, long height)
    {
        if (width <= 0 || height <= 0 || width * height > MaximumPixelCount)
            throw new InvalidDataException($"The image is {width} by {height} pixels; images must have between 1 and {MaximumPixelCount} pixels");
    }
}
