#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Gibbed.RefPack;
#pragma warning restore IDE0130 // Namespace does not match folder structure

static class ByteHelpers
{
    public static bool RefPackCompress(this byte[] input, out byte[]? output, CompressionLevel level) =>
        Compression.Compress(input, out output, level);

    public static bool RefPackCompress(this byte[] input, out byte[]? output) =>
        Compression.Compress(input, out output, CompressionLevel.Max);

    public static byte[] RefPackDecompress(this byte[] input) =>
        Decompression.Decompress(input);
}
