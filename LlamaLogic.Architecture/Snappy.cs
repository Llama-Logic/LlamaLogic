namespace LlamaLogic.Architecture;

static class Snappy
{
    const int hashBits = 14;
    const int maximumExpansion = 22;
    const int maximumOffset = 65535;
    const int minimumMatch = 4;

    public static byte[] Compress(ReadOnlySpan<byte> data)
    {
        var output = new ArrayBufferWriter<byte>(data.Length / 2 + 16);
        WriteVarint(output, (uint)data.Length);
        var table = new int[1 << hashBits];
        Array.Fill(table, -1);
        var literalStart = 0;
        var position = 0;
        while (position + minimumMatch <= data.Length)
        {
            var hash = Hash(data[position..]);
            var candidate = table[hash];
            table[hash] = position;
            if (candidate >= 0 && position - candidate <= maximumOffset && data.Slice(candidate, minimumMatch).SequenceEqual(data.Slice(position, minimumMatch)))
            {
                var length = minimumMatch;
                while (position + length < data.Length && data[candidate + length] == data[position + length])
                    ++length;
                WriteLiteral(output, data[literalStart..position]);
                WriteCopy(output, position - candidate, length);
                position += length;
                literalStart = position;
                continue;
            }
            ++position;
        }
        WriteLiteral(output, data[literalStart..]);
        return output.WrittenSpan.ToArray();
    }

    static void Copy(byte[] output, ref int written, int offset, int length)
    {
        if (offset <= 0 || offset > written)
            throw new InvalidDataException($"A Snappy copy has an offset of {offset} at {written} bytes into its output");
        if (length > output.Length - written)
            throw new InvalidDataException("A Snappy copy extends beyond the decompressed length");
        for (var i = 0; i < length; ++i)
            output[written + i] = output[written - offset + i];
        written += length;
    }

    public static byte[] Decompress(ReadOnlySpan<byte> data)
    {
        var position = 0;
        var length = ReadVarint(data, ref position);
        if (length > (long)(data.Length - position) * maximumExpansion)
            throw new InvalidDataException($"The Snappy data claims {length} bytes, more than its {data.Length - position} bytes of elements can produce");
        var output = new byte[length];
        var written = 0;
        while (position < data.Length)
        {
            var tag = data[position++];
            switch (tag & 3)
            {
                case 0:
                {
                    var literalLength = (tag >> 2) + 1;
                    if (literalLength > 60)
                    {
                        var byteCount = literalLength - 60;
                        if (position + byteCount > data.Length)
                            throw new InvalidDataException("The Snappy data ends inside a literal length");
                        var storedLength = 0U;
                        for (var i = 0; i < byteCount; ++i)
                            storedLength |= (uint)data[position + i] << (8 * i);
                        position += byteCount;
                        if (storedLength >= int.MaxValue)
                            throw new InvalidDataException("A Snappy literal is too long");
                        literalLength = (int)storedLength + 1;
                    }
                    if (literalLength > data.Length - position || literalLength > output.Length - written)
                        throw new InvalidDataException("A Snappy literal extends beyond its data or the decompressed length");
                    data.Slice(position, literalLength).CopyTo(output.AsSpan(written));
                    position += literalLength;
                    written += literalLength;
                    break;
                }
                case 1:
                {
                    if (position >= data.Length)
                        throw new InvalidDataException("The Snappy data ends inside a copy");
                    var copyLength = ((tag >> 2) & 7) + 4;
                    var offset = ((tag >> 5) << 8) | data[position++];
                    Copy(output, ref written, offset, copyLength);
                    break;
                }
                case 2:
                {
                    if (position + 2 > data.Length)
                        throw new InvalidDataException("The Snappy data ends inside a copy");
                    var offset = BinaryPrimitives.ReadUInt16LittleEndian(data[position..]);
                    position += 2;
                    Copy(output, ref written, offset, (tag >> 2) + 1);
                    break;
                }
                default:
                {
                    if (position + 4 > data.Length)
                        throw new InvalidDataException("The Snappy data ends inside a copy");
                    var offset = BinaryPrimitives.ReadUInt32LittleEndian(data[position..]);
                    position += 4;
                    if (offset > int.MaxValue)
                        throw new InvalidDataException($"A Snappy copy has an offset of {offset}");
                    Copy(output, ref written, (int)offset, (tag >> 2) + 1);
                    break;
                }
            }
        }
        if (written != output.Length)
            throw new InvalidDataException($"The Snappy data decompresses to {written} bytes, but claims {output.Length}");
        return output;
    }

    static int Hash(ReadOnlySpan<byte> data) =>
        (int)((BinaryPrimitives.ReadUInt32LittleEndian(data) * 0x1e35a7bdU) >> (32 - hashBits));

    static uint ReadVarint(ReadOnlySpan<byte> data, ref int position)
    {
        var result = 0U;
        for (var shift = 0; shift < 35; shift += 7)
        {
            if (position >= data.Length)
                throw new InvalidDataException("The Snappy data ends inside its length");
            var value = data[position++];
            if (shift == 28 && value > 0x0f)
                throw new InvalidDataException("The Snappy length does not fit in 32 bits");
            result |= (uint)(value & 0x7f) << shift;
            if ((value & 0x80) == 0)
                return result;
        }
        throw new InvalidDataException("The Snappy length does not fit in 32 bits");
    }

    static void WriteCopy(ArrayBufferWriter<byte> output, int offset, int length)
    {
        while (length > 0)
        {
            var chunk = Math.Min(length, 64);
            var span = output.GetSpan(3);
            span[0] = (byte)(((chunk - 1) << 2) | 2);
            BinaryPrimitives.WriteUInt16LittleEndian(span[1..], (ushort)offset);
            output.Advance(3);
            length -= chunk;
        }
    }

    static void WriteLiteral(ArrayBufferWriter<byte> output, ReadOnlySpan<byte> literal)
    {
        while (!literal.IsEmpty)
        {
            var chunk = Math.Min(literal.Length, 65536);
            if (chunk <= 60)
            {
                output.GetSpan(1)[0] = (byte)((chunk - 1) << 2);
                output.Advance(1);
            }
            else if (chunk <= 256)
            {
                var span = output.GetSpan(2);
                span[0] = 60 << 2;
                span[1] = (byte)(chunk - 1);
                output.Advance(2);
            }
            else
            {
                var span = output.GetSpan(3);
                span[0] = 61 << 2;
                BinaryPrimitives.WriteUInt16LittleEndian(span[1..], (ushort)(chunk - 1));
                output.Advance(3);
            }
            literal[..chunk].CopyTo(output.GetSpan(chunk));
            output.Advance(chunk);
            literal = literal[chunk..];
        }
    }

    static void WriteVarint(ArrayBufferWriter<byte> output, uint value)
    {
        while (value >= 0x80)
        {
            output.GetSpan(1)[0] = (byte)(value | 0x80);
            output.Advance(1);
            value >>= 7;
        }
        output.GetSpan(1)[0] = (byte)value;
        output.Advance(1);
    }
}
