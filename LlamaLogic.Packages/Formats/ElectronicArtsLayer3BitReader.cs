namespace LlamaLogic.Packages.Formats;

struct ElectronicArtsLayer3BitReader(byte[] data, int position)
{
    public int Position { get; private set; } = position;

    public uint Read(int count)
    {
        var value = 0UL;
        var remaining = count;
        while (remaining > 0)
        {
            var index = Position >> 3;
            if (index >= data.Length)
                throw new InvalidDataException("An EALayer3 frame is cut off");
            var available = 8 - (Position & 7);
            var take = Math.Min(available, remaining);
            value = (value << take) | (uint)((data[index] >> (available - take)) & ((1 << take) - 1));
            Position += take;
            remaining -= take;
        }
        return (uint)value;
    }
}