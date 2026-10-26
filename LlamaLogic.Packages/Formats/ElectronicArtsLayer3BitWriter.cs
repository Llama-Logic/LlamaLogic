namespace LlamaLogic.Packages.Formats;

sealed class ElectronicArtsLayer3BitWriter
{
    byte[] buffer = new byte[2048];

    public int BitPosition { get; private set; }

    public void Align()
    {
        if ((BitPosition & 7) is not 0)
            Write(8 - (BitPosition & 7), 0);
    }

    public void Copy(ref ElectronicArtsLayer3BitReader reader, int count)
    {
        while (count >= 32)
        {
            Write(32, reader.Read(32));
            count -= 32;
        }
        if (count > 0)
            Write(count, reader.Read(count));
    }

    public ReadOnlySpan<byte> GetWrittenBytes() =>
        buffer.AsSpan(0, (BitPosition + 7) >> 3);

    public void Reset()
    {
        Array.Clear(buffer, 0, (BitPosition + 7) >> 3);
        BitPosition = 0;
    }

    public void Write(int count, uint value)
    {
        var needed = (BitPosition + count + 7) >> 3;
        if (needed > buffer.Length)
            Array.Resize(ref buffer, Math.Max(needed, buffer.Length * 2));
        while (count > 0)
        {
            var index = BitPosition >> 3;
            var available = 8 - (BitPosition & 7);
            var take = Math.Min(available, count);
            var bits = (int)((value >> (count - take)) & ((1u << take) - 1));
            buffer[index] |= (byte)(bits << (available - take));
            BitPosition += take;
            count -= take;
        }
    }
}