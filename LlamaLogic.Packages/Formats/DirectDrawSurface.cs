using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using BCnEncoder.Shared.ImageFiles;

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
    public static ReadOnlyMemory<byte> GetPngDataFromDdsData(ReadOnlyMemory<byte> ddsData)
    {
        ddsData = Swizzle(ddsData, false);
        using var ddsDataStream = new ReadOnlyMemoryOfByteStream(ddsData);
        var decoder = new BcDecoder();
        using var ddsImage = decoder.DecodeToImageRgba32(ddsDataStream);
        using var pngDataStream = new ArrayBufferWriterOfByteStream();
        ddsImage.SaveAsPng(pngDataStream);
        return pngDataStream.WrittenMemory;
    }

    /// <summary>
    /// Convert a DirectDraw Surface to a Portable Network Graphic, asynchronously
    /// </summary>
    /// <param name="ddsData">The data of a DirectDraw Surface</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    public static async Task<ReadOnlyMemory<byte>> GetPngDataFromDdsDataAsync(ReadOnlyMemory<byte> ddsData, CancellationToken cancellationToken = default)
    {
        ddsData = Swizzle(ddsData, false);
        using var ddsDataStream = new ReadOnlyMemoryOfByteStream(ddsData);
        var decoder = new BcDecoder();
        using var ddsImage = await decoder.DecodeToImageRgba32Async(ddsDataStream, token: cancellationToken).ConfigureAwait(false);
        using var pngDataStream = new ArrayBufferWriterOfByteStream();
        await ddsImage.SaveAsPngAsync(pngDataStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        return pngDataStream.WrittenMemory;
    }

    /// <summary>
    /// Convert a Portable Network Graphic to a shuffled DirectDraw Surface compatible with The Sims 4
    /// </summary>
    /// <param name="pngData">The data of the Portable Network Graphic</param>
    public static ReadOnlyMemory<byte> GetDstDataFromPngData(ReadOnlyMemory<byte> pngData)
    {
        var encoder = new BcEncoder();
        DdsFile ddsImage;
        using (var pngDataStream = new ReadOnlyMemoryOfByteStream(pngData))
        using (var pngImage = Image.Load<Rgba32>(pngDataStream))
        {
            encoder.OutputOptions.Format = CompressionFormat.Bc3;
            ddsImage = encoder.EncodeToDds(pngImage);
        }
        using var ddsDataStream = new ArrayBufferWriterOfByteStream();
        ddsImage.Write(ddsDataStream);
        return Swizzle(ddsDataStream.WrittenMemory[..(int)ddsDataStream.Length], true);
    }

    /// <summary>
    /// Convert a Portable Network Graphic to a shuffled DirectDraw Surface compatible with The Sims 4, asynchronously
    /// </summary>
    /// <param name="pngData">The data of the Portable Network Graphic</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    public static async Task<ReadOnlyMemory<byte>> GetDstDataFromPngDataAsync(ReadOnlyMemory<byte> pngData, CancellationToken cancellationToken = default)
    {
        var encoder = new BcEncoder();
        DdsFile ddsImage;
        using (var pngDataStream = new ReadOnlyMemoryOfByteStream(pngData))
        using (var pngImage = await Image.LoadAsync<Rgba32>(pngDataStream, cancellationToken).ConfigureAwait(false))
        {
            encoder.OutputOptions.Format = CompressionFormat.Bc3;
            ddsImage = await encoder.EncodeToDdsAsync(pngImage, token: cancellationToken).ConfigureAwait(false);
        }
        using var ddsDataStream = new ArrayBufferWriterOfByteStream();
        ddsImage.Write(ddsDataStream);
        return Swizzle(ddsDataStream.WrittenMemory[..(int)ddsDataStream.Length], true);
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