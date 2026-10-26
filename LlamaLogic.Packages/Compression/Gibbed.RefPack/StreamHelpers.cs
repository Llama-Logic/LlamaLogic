/* Copyright (c) 2013 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Gibbed.RefPack;
#pragma warning restore IDE0130 // Namespace does not match folder structure

static class StreamHelpers
{
    public static bool RefPackCompress(this Stream input, int length, out byte[]? output, CompressionLevel level)
    {
        var data = new byte[length];
        if (input.Read(data, 0, data.Length) != data.Length)
        {
            throw new EndOfStreamException();
        }
        return Compression.Compress(data, out output, level);
    }

    public static bool RefPackCompress(this Stream input, int length, out byte[]? output)
    {
        var data = new byte[length];
        if (input.Read(data, 0, data.Length) != data.Length)
        {
            throw new EndOfStreamException();
        }
        return Compression.Compress(data, out output, CompressionLevel.Max);
    }

    public static byte[] RefPackDecompress(this Stream input) =>
        Decompression.Decompress(input);
}
