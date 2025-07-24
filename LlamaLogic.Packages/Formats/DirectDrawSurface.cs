namespace LlamaLogic.Packages.Formats;

/// <summary>
/// Provides methods for dealing with diffuse surface textures in Direct Draw Surfaces
/// </summary>
/// <remarks>
/// Adapted from Gibbed.Sims 4 by Rick
/// https://github.com/gibbed/Gibbed.Sims4
/// </remarks>
public static class DirectDrawSurface
{
    [Flags]
    enum HeaderFlags :
        uint
    {
        Texture = 0x00001007, // DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT 
        MipMap = 0x00020000, // DDSD_MIPMAPCOUNT
        Volume = 0x00800000, // DDSD_DEPTH
        Pitch = 0x00000008, // DDSD_PITCH
        LinerSize = 0x00080000, // DDSD_LINEARSIZE
    }

    [Flags]
    enum PixelFormatFlags :
        uint
    {
        FourCC = 0x00000004,
        RGB = 0x00000040,
        RGBA = 0x00000041,
        Luminance = 0x00020000,
    }

    const int dataOffset = 128;
    const uint dst1 = 0x31545344;
    const uint dst3 = 0x33545344;
    const uint dst5 = 0x35545344;
    const uint dxt1 = 0x31545844;
    const uint dxt3 = 0x33545844;
    const uint dxt5 = 0x35545844;
    const uint headerSignature = 0x20534444;
    const uint supportedSize = 18 * sizeof(uint) + supportedPixelFormatSize + 5 * sizeof(uint);
    const uint supportedPixelFormatSize = 8 * sizeof(uint);

    /// <summary>
    /// Convert the data of a DST/DXT DirectDraw Surface image data to Portable Network Graphic data
    /// </summary>
    /// <param name="ddsData">The data of a DST/DXT DirectDraw Surface image</param>
    public static ReadOnlyMemory<byte> GetPngDataFromDiffuseSurfaceTextureData(ReadOnlyMemory<byte> ddsData)
    {
        if (ddsData.Length < 128)
            throw new EndOfStreamException("data is not large enough to be a DDS image");
        uint position = 0;
        var dataHeaderSignature = ddsData.ReadAndAdvancePosition<uint>(ref position);
        var endianness =
            dataHeaderSignature == headerSignature
            ? Endianness.Little
            : BinaryPrimitives.ReverseEndianness(dataHeaderSignature) == headerSignature
            ? Endianness.Big
            : Endianness.None;
        if (endianness is Endianness.None)
            throw new FormatException("this is not a DDS image");
        var size = ddsData.ReadAndAdvancePosition<uint>(ref position);
        var flags = ddsData.ReadAndAdvancePosition<HeaderFlags>(ref position);
        position += 16 * sizeof(uint); // height + width + pitchOrLinearSize + depth + mipMapCount + reserved 1
        var pixelFormatSize = ddsData.ReadAndAdvancePosition<uint>(ref position);
        position += 1 * sizeof(uint); // pixelFormatFlags;
        var pixelFormatFourCC = ddsData.ReadAndAdvancePosition<uint>(ref position);
        position += 8 * sizeof(uint); // pixelFormatRGBBitCount + pixelFormatRedBitMask + pixelFormatGreenBitMask + pixelFormatBlueBitMask + pixelFormatAlphaBitMask + surfaceFlags + cubeMapFlags + reserved 2
        if (endianness is Endianness.Big)
        {
            size = BinaryPrimitives.ReverseEndianness(size);
            flags = (HeaderFlags)BinaryPrimitives.ReverseEndianness((uint)flags);
            pixelFormatSize = BinaryPrimitives.ReverseEndianness(pixelFormatSize);
            pixelFormatFourCC = BinaryPrimitives.ReverseEndianness(pixelFormatFourCC);
        }
        if (size != supportedSize)
            throw new NotSupportedException("unsupported header size");
        if (pixelFormatSize != supportedPixelFormatSize)
            throw new NotSupportedException("unsupported pixel format size");
        if (!flags.HasFlag(HeaderFlags.Texture))
            throw new NotSupportedException("unsupported header flags");
        if (pixelFormatFourCC is dst1 or dst3 or dst5)
        {
            using var unshuffled = new ArrayBufferWriterOfByteStream(ddsData.Length);
            unshuffled.Write(ddsData.Span[0..84]);
            var newPixelFormatFourCC = pixelFormatFourCC switch { dst3 => dxt3, dst5 => dxt5, _ => dxt1 };
            if (endianness is Endianness.Big)
                newPixelFormatFourCC = BinaryPrimitives.ReverseEndianness(newPixelFormatFourCC);
            Span<byte> newPixelFormatFourCCSpan = stackalloc byte[4];
            MemoryMarshal.Write(newPixelFormatFourCCSpan, ref newPixelFormatFourCC);
            unshuffled.Write(newPixelFormatFourCCSpan);
            unshuffled.Write(ddsData.Span[88..128]);
            var dataSpan = ddsData.Span[dataOffset..];
            if (pixelFormatFourCC is dst1)
            {
                var blockOffset2 = 0;
                var blockOffset3 = blockOffset2 + (dataSpan.Length >> 1);
                var count = (blockOffset3 - blockOffset2) / 4;
                for (var i = 0; i < count; ++i)
                {
                    unshuffled.Write(dataSpan[blockOffset2..(blockOffset2 + 4)]);
                    unshuffled.Write(dataSpan[blockOffset3..(blockOffset3 + 4)]);
                    blockOffset2 += 4;
                    blockOffset3 += 4;
                }
            }
            else if (pixelFormatFourCC is dst3)
            {
                var blockOffset0 = 0;
                var blockOffset2 = blockOffset0 + (dataSpan.Length >> 1);
                var blockOffset3 = blockOffset2 + (dataSpan.Length >> 2);
                var count = (blockOffset3 - blockOffset2) / 4;
                for (var i = 0; i < count; ++i)
                {
                    unshuffled.Write(dataSpan[blockOffset0..(blockOffset0 + 8)]);
                    unshuffled.Write(dataSpan[blockOffset2..(blockOffset2 + 4)]);
                    unshuffled.Write(dataSpan[blockOffset3..(blockOffset3 + 4)]);
                    blockOffset0 += 8;
                    blockOffset2 += 4;
                    blockOffset3 += 4;
                }
            }
            else if (pixelFormatFourCC is dst5)
            {
                var blockOffset0 = 0;
                var blockOffset2 = blockOffset0 + (dataSpan.Length >> 3);
                var blockOffset1 = blockOffset2 + (dataSpan.Length >> 2);
                var blockOffset3 = blockOffset1 + (6 * dataSpan.Length >> 4);
                var count = (blockOffset2 - blockOffset0) / 2;
                for (var i = 0; i < count; ++i)
                {
                    unshuffled.Write(dataSpan[blockOffset0..(blockOffset0 + 2)]);
                    unshuffled.Write(dataSpan[blockOffset1..(blockOffset1 + 6)]);
                    unshuffled.Write(dataSpan[blockOffset2..(blockOffset2 + 4)]);
                    unshuffled.Write(dataSpan[blockOffset3..(blockOffset3 + 4)]);
                    blockOffset0 += 2;
                    blockOffset1 += 6;
                    blockOffset2 += 4;
                    blockOffset3 += 4;
                }
            }
            ddsData = unshuffled.WrittenMemory;
        }
        using var ddsDataStream = new ReadOnlyMemoryOfByteStream(ddsData);
        var decoder = new BcDecoder();
        using var ddsImage = decoder.DecodeToImageRgba32(ddsDataStream);
        using var pngDataStream = new ArrayBufferWriterOfByteStream();
        ddsImage.SaveAsPng(pngDataStream);
        return pngDataStream.WrittenMemory;
    }

    /// <summary>
    /// Convert the data of a DST/DXT DirectDraw Surface image data to Portable Network Graphic data, asynchronously
    /// </summary>
    /// <param name="ddsData">The data of a DST/DXT DirectDraw Surface image</param>
    public static async Task<ReadOnlyMemory<byte>> GetPngDataFromDiffuseSurfaceTextureDataAsync(ReadOnlyMemory<byte> ddsData)
    {
        if (ddsData.Length < 128)
            throw new EndOfStreamException("data is not large enough to be a DDS image");
        uint position = 0;
        var dataHeaderSignature = ddsData.ReadAndAdvancePosition<uint>(ref position);
        var endianness =
            dataHeaderSignature == headerSignature
            ? Endianness.Little
            : BinaryPrimitives.ReverseEndianness(dataHeaderSignature) == headerSignature
            ? Endianness.Big
            : Endianness.None;
        if (endianness is Endianness.None)
            throw new FormatException("this is not a DDS image");
        var size = ddsData.ReadAndAdvancePosition<uint>(ref position);
        var flags = ddsData.ReadAndAdvancePosition<HeaderFlags>(ref position);
        position += 16 * sizeof(uint); // height + width + pitchOrLinearSize + depth + mipMapCount + reserved 1
        var pixelFormatSize = ddsData.ReadAndAdvancePosition<uint>(ref position);
        position += 1 * sizeof(uint); // pixelFormatFlags;
        var pixelFormatFourCC = ddsData.ReadAndAdvancePosition<uint>(ref position);
        position += 8 * sizeof(uint); // pixelFormatRGBBitCount + pixelFormatRedBitMask + pixelFormatGreenBitMask + pixelFormatBlueBitMask + pixelFormatAlphaBitMask + surfaceFlags + cubeMapFlags + reserved 2
        if (endianness is Endianness.Big)
        {
            size = BinaryPrimitives.ReverseEndianness(size);
            flags = (HeaderFlags)BinaryPrimitives.ReverseEndianness((uint)flags);
            pixelFormatSize = BinaryPrimitives.ReverseEndianness(pixelFormatSize);
            pixelFormatFourCC = BinaryPrimitives.ReverseEndianness(pixelFormatFourCC);
        }
        if (size != supportedSize)
            throw new NotSupportedException("unsupported header size");
        if (pixelFormatSize != supportedPixelFormatSize)
            throw new NotSupportedException("unsupported pixel format size");
        if (!flags.HasFlag(HeaderFlags.Texture))
            throw new NotSupportedException("unsupported header flags");
        if (pixelFormatFourCC is dst1 or dst3 or dst5)
        {
            using var unshuffled = new ArrayBufferWriterOfByteStream();
            await unshuffled.WriteAsync(ddsData[0..84]).ConfigureAwait(false);
            var newPixelFormatFourCC = pixelFormatFourCC switch { dst3 => dxt3, dst5 => dxt5, _ => dxt1 };
            if (endianness is Endianness.Big)
                newPixelFormatFourCC = BinaryPrimitives.ReverseEndianness(newPixelFormatFourCC);
            var newPixelFormatFourCCArray = ArrayPool<byte>.Shared.Rent(4);
            try
            {
                Span<byte> newPixelFormatFourCCSpan = newPixelFormatFourCCArray;
                MemoryMarshal.Write(newPixelFormatFourCCSpan[..4], ref newPixelFormatFourCC);
                unshuffled.Write(newPixelFormatFourCCSpan[..4]);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(newPixelFormatFourCCArray);
            }
            unshuffled.Write(ddsData.Span[88..128]);
            var dataMemory = ddsData[dataOffset..];
            if (pixelFormatFourCC is dst1)
            {
                var blockOffset2 = 0;
                var blockOffset3 = blockOffset2 + (dataMemory.Length >> 1);
                var count = (blockOffset3 - blockOffset2) / 4;
                for (var i = 0; i < count; ++i)
                {
                    await unshuffled.WriteAsync(dataMemory[blockOffset2..(blockOffset2 + 4)]).ConfigureAwait(false);
                    await unshuffled.WriteAsync(dataMemory[blockOffset3..(blockOffset3 + 4)]).ConfigureAwait(false);
                    blockOffset2 += 4;
                    blockOffset3 += 4;
                }
            }
            else if (pixelFormatFourCC is dst3)
                throw new NotImplementedException("no samples");
            else if (pixelFormatFourCC is dst5)
            {
                var blockOffset0 = 0;
                var blockOffset2 = blockOffset0 + (dataMemory.Length >> 3);
                var blockOffset1 = blockOffset2 + (dataMemory.Length >> 2);
                var blockOffset3 = blockOffset1 + (6 * dataMemory.Length >> 4);
                var count = (blockOffset2 - blockOffset0) / 2;
                for (var i = 0; i < count; ++i)
                {
                    await unshuffled.WriteAsync(dataMemory[blockOffset0..(blockOffset0 + 2)]).ConfigureAwait(false);
                    await unshuffled.WriteAsync(dataMemory[blockOffset1..(blockOffset1 + 6)]).ConfigureAwait(false);
                    await unshuffled.WriteAsync(dataMemory[blockOffset2..(blockOffset2 + 4)]).ConfigureAwait(false);
                    await unshuffled.WriteAsync(dataMemory[blockOffset3..(blockOffset3 + 4)]).ConfigureAwait(false);
                    blockOffset0 += 2;
                    blockOffset1 += 6;
                    blockOffset2 += 4;
                    blockOffset3 += 4;
                }
            }
            ddsData = unshuffled.WrittenMemory;
        }
        using var ddsDataStream = new ReadOnlyMemoryOfByteStream(ddsData);
        var decoder = new BcDecoder();
        using var ddsImage = await decoder.DecodeToImageRgba32Async(ddsDataStream).ConfigureAwait(false);
        using var pngDataStream = new ArrayBufferWriterOfByteStream();
        await ddsImage.SaveAsPngAsync(pngDataStream).ConfigureAwait(false);
        return pngDataStream.WrittenMemory;
    }
}
