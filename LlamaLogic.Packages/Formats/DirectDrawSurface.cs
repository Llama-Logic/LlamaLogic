namespace LlamaLogic.Packages.Formats;

/// <summary>
/// Provides methods for dealing with diffuse surface textures in Direct Draw Surfaces
/// </summary>
/// <remarks>
/// Adapted from S4S by Andrew
/// </remarks>
public static class DirectDrawSurface
{
    /// <summary>
    /// Convert a DirectDraw Surface to a Portable Network Graphic
    /// </summary>
    /// <param name="ddsData">The data of a DirectDraw Surface</param>
    /// <exception cref="InvalidDataException">The surface has no pixels or more than 67,108,864 (8,192 by 8,192) pixels</exception>
    public static ReadOnlyMemory<byte> GetPngDataFromDdsData(ReadOnlyMemory<byte> ddsData)
    {
        RequireSupportedSize(ddsData.Span);
        ddsData = Swizzle(ddsData, false);
        using var ddsDataStream = new ReadOnlyMemoryOfByteStream(ddsData);
        var decoder = new BcDecoder();
        return EncodePng(decoder.Decode2D(ddsDataStream));
    }

    /// <summary>
    /// Convert a DirectDraw Surface to a Portable Network Graphic, asynchronously
    /// </summary>
    /// <param name="ddsData">The data of a DirectDraw Surface</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    /// <exception cref="InvalidDataException">The surface has no pixels or more than 67,108,864 (8,192 by 8,192) pixels</exception>
    public static async Task<ReadOnlyMemory<byte>> GetPngDataFromDdsDataAsync(ReadOnlyMemory<byte> ddsData, CancellationToken cancellationToken = default)
    {
        RequireSupportedSize(ddsData.Span);
        ddsData = Swizzle(ddsData, false);
        using var ddsDataStream = new ReadOnlyMemoryOfByteStream(ddsData);
        var decoder = new BcDecoder();
        return EncodePng(await decoder.Decode2DAsync(ddsDataStream, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Convert a Portable Network Graphic to a shuffled DirectDraw Surface compatible with The Sims 4
    /// </summary>
    /// <param name="pngData">The data of the Portable Network Graphic</param>
    /// <exception cref="InvalidDataException"><paramref name="pngData"/> is not a PNG, could not be decoded, or has more than 67,108,864 (8,192 by 8,192) pixels</exception>
    public static ReadOnlyMemory<byte> GetDstDataFromPngData(ReadOnlyMemory<byte> pngData)
    {
        var pixels = ImageDecoding.DecodePngAsRgba(pngData, out var width, out var height);
        var encoder = new BcEncoder();
        encoder.OutputOptions.Format = CompressionFormat.Bc3;
        var ddsImage = encoder.EncodeToDds(pixels, width, height, PixelFormat.Rgba32);
        using var ddsDataStream = new ArrayBufferWriterOfByteStream();
        ddsImage.Write(ddsDataStream);
        return Swizzle(ddsDataStream.WrittenMemory[..(int)ddsDataStream.Length], true);
    }

    /// <summary>
    /// Convert a Portable Network Graphic to a shuffled DirectDraw Surface compatible with The Sims 4, asynchronously
    /// </summary>
    /// <param name="pngData">The data of the Portable Network Graphic</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    /// <exception cref="InvalidDataException"><paramref name="pngData"/> is not a PNG, could not be decoded, or has more than 67,108,864 (8,192 by 8,192) pixels</exception>
    public static async Task<ReadOnlyMemory<byte>> GetDstDataFromPngDataAsync(ReadOnlyMemory<byte> pngData, CancellationToken cancellationToken = default)
    {
        var pixels = ImageDecoding.DecodePngAsRgba(pngData, out var width, out var height);
        var encoder = new BcEncoder();
        encoder.OutputOptions.Format = CompressionFormat.Bc3;
        var ddsImage = await encoder.EncodeToDdsAsync(pixels, width, height, PixelFormat.Rgba32, cancellationToken).ConfigureAwait(false);
        using var ddsDataStream = new ArrayBufferWriterOfByteStream();
        ddsImage.Write(ddsDataStream);
        return Swizzle(ddsDataStream.WrittenMemory[..(int)ddsDataStream.Length], true);
    }

    static ReadOnlyMemory<byte> EncodePng(Memory2D<ColorRgba32> pixels)
    {
        var rgba = new byte[pixels.Width * pixels.Height * 4];
        var rgbaColors = MemoryMarshal.Cast<byte, ColorRgba32>(rgba.AsSpan());
        for (var y = 0; y < pixels.Height; ++y)
            pixels.Span.GetRowSpan(y).CopyTo(rgbaColors.Slice(y * pixels.Width, pixels.Width));
        return ImageEncoding.EncodeRgbaPng(rgba, pixels.Width, pixels.Height);
    }

    static void RequireSupportedSize(ReadOnlySpan<byte> ddsData)
    {
        if (ddsData.Length >= 20 && ddsData[..4].SequenceEqual("DDS "u8))
            ImageDecoding.RequireSupportedSize(BinaryPrimitives.ReadUInt32LittleEndian(ddsData[16..20]), BinaryPrimitives.ReadUInt32LittleEndian(ddsData[12..16]));
    }

    static ReadOnlyMemory<byte> Swizzle(ReadOnlyMemory<byte> ddsData, bool shouldSwizzle)
    {
        var fourCc = MemoryMarshal.Read<uint>(ddsData[0x54..0x58].Span);
        if (!SwizzleInfo.Data.TryGetValue(fourCc, out var si)
            || shouldSwizzle == si.Swizzled)
            return ddsData;
        var source = ddsData[0x80..];
        var targetArray = ArrayPool<byte>.Shared.Rent(source.Length);
        try
        {
            Memory<byte> targetArrayMemory = targetArray;
            var target = targetArrayMemory[..source.Length];
            var blockCount = source.Length / si.BlockSize;
            var blockOffsets = new int[si.SegmentLengths.Length];
            var start = 0;
            for (var i = 0; i < si.SegmentLengths.Length; ++i)
            {
                blockOffsets[i] = start;
                start += blockCount * si.SegmentLengths[i];
            }
            int sourceOffset = 0, targetOffset = 0, tempOffset;
            for (var i = 0; i < blockCount; ++i)
            {
                tempOffset = si.BlockSize * i;
                if (si.Swizzled)
                    targetOffset = tempOffset;
                else
                    sourceOffset = tempOffset;
                for (var j = 0; j < si.SegmentLengths.Length; ++j)
                {
                    var blockLength = si.SegmentLengths[si.SegmentOrders[j]];
                    tempOffset = blockOffsets[si.SegmentOrders[j]] + i * blockLength;
                    if (si.Swizzled)
                        sourceOffset = tempOffset;
                    else
                        targetOffset = tempOffset;
                    source[sourceOffset..(sourceOffset + blockLength)].CopyTo(target[targetOffset..(targetOffset + blockLength)]);
                    if (si.Swizzled)
                        targetOffset += blockLength;
                    else
                        sourceOffset += blockLength;
                }
            }
            Memory<byte> reconstituted = new byte[ddsData.Length];
            ddsData[..0x54].CopyTo(reconstituted[..0x54]);
            var newFourCc = si.FourCC;
            MemoryMarshal.Write(reconstituted[0x54..0x58].Span, ref newFourCc);
            ddsData[0x58..0x80].CopyTo(reconstituted[0x58..0x80]);
            target.CopyTo(reconstituted[0x80..]);
            return reconstituted;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(targetArray);
        }
    }
}

file class SwizzleInfo
{
    public static Dictionary<uint, SwizzleInfo> Data = [];

    static SwizzleInfo()
    {
        Register("DXT1", "DST1", [0, 1], [4, 4]);
        Register("DXT3", "DST3", [0, 1, 2], [8, 4, 4]);
        Register("DXT5", "DST5", [0, 2, 1, 3], [2, 4, 6, 4]);
    }

    static uint Signature(string chars) =>
        BitConverter.ToUInt32(Encoding.ASCII.GetBytes(chars));

    static string Signature(uint fourcc) =>
        Encoding.ASCII.GetString(BitConverter.GetBytes(fourcc));

    public SwizzleInfo(int[] segmentOrders, int[] segmentLengths, uint fourCc, bool swizzled = false)
    {
        SegmentOrders = segmentOrders;
        SegmentLengths = segmentLengths;
        Swizzled = swizzled;
        FourCC = fourCc;
        BlockSize = SegmentLengths.Sum();
        BlockCount = SegmentLengths.Length;
    }

    public int BlockSize { get; }
    public int BlockCount { get; }
    public bool Swizzled { get; }
    public int[] SegmentLengths { get; }
    public int[] SegmentOrders { get; }
    public uint FourCC { get; }

    public override string ToString() => $"{Signature(FourCC)} {Swizzled}";

    public static void Register(string baseFormat, string shuffledFormat, int[] segmentOrders, int[] segmentLengths)
    {
        Data[Signature(baseFormat)] = new SwizzleInfo(segmentOrders, segmentLengths, Signature(shuffledFormat));
        Data[Signature(shuffledFormat)] = new SwizzleInfo(segmentOrders, segmentLengths, Signature(baseFormat), true);
    }
}