namespace LlamaLogic.Architecture;

sealed class SectionWriter
{
    public SectionWriter(LotArchitectureSectionType type) =>
        Type = type;

    readonly ArrayBufferWriter<byte> buffer = new();

    public List<ResourceKey>? KeyCollector { get; init; }

    public int Position =>
        buffer.WrittenCount;

    public LotArchitectureSectionType Type { get; }

    public ReadOnlyMemory<byte> WrittenMemory =>
        buffer.WrittenMemory;

    Span<byte> Advance(int count)
    {
        var span = buffer.GetSpan(count)[..count];
        buffer.Advance(count);
        return span;
    }

    public InvalidOperationException Invalid(string message) =>
        new($"The {Type} section cannot be encoded: {message}");

    public void PatchInt32(int position, int value) =>
        BinaryPrimitives.WriteInt32LittleEndian(MemoryMarshal.AsMemory(buffer.WrittenMemory).Span[position..], value);

    public void RequireDefault<T>(T value, T defaultValue, string description, uint version)
    {
        if (!EqualityComparer<T>.Default.Equals(value, defaultValue))
            throw Invalid($"{description} is {value}, which version {version} cannot store");
    }

    public void RequireDefault(float value, float defaultValue, string description, uint version)
    {
        if (BitConverter.SingleToInt32Bits(value) != BitConverter.SingleToInt32Bits(defaultValue))
            throw Invalid($"{description} is {value.ToString(CultureInfo.InvariantCulture)}, which version {version} cannot store");
    }

    public void RequireEmpty(int count, string description, uint version)
    {
        if (count != 0)
            throw Invalid($"{description} holds {count} entries, which version {version} cannot store");
    }

    public void Write(bool value) =>
        Write(value ? (byte)1 : (byte)0);

    public void Write(byte value) =>
        Advance(1)[0] = value;

    public void Write(ReadOnlySpan<byte> value) =>
        value.CopyTo(Advance(value.Length));

    public void Write(short value) =>
        BinaryPrimitives.WriteInt16LittleEndian(Advance(2), value);

    public void Write(int value) =>
        BinaryPrimitives.WriteInt32LittleEndian(Advance(4), value);

    public void Write(long value) =>
        BinaryPrimitives.WriteInt64LittleEndian(Advance(8), value);

    public void Write(sbyte value) =>
        Advance(1)[0] = unchecked((byte)value);

    public void Write(float value) =>
        BinaryPrimitives.WriteSingleLittleEndian(Advance(4), value);

    public void Write(ushort value) =>
        BinaryPrimitives.WriteUInt16LittleEndian(Advance(2), value);

    public void Write(uint value) =>
        BinaryPrimitives.WriteUInt32LittleEndian(Advance(4), value);

    public void Write(ulong value) =>
        BinaryPrimitives.WriteUInt64LittleEndian(Advance(8), value);

    public void WriteCount(int count) =>
        Write(count);

    public void WriteKeyInstanceTypeGroup(ResourceKey key)
    {
        KeyCollector?.Add(key);
        Write(key.FullInstance);
        Write((uint)key.Type);
        Write(key.Group);
    }

    public void WriteKeyTypeGroupInstance(ResourceKey key)
    {
        KeyCollector?.Add(key);
        Write((uint)key.Type);
        Write(key.Group);
        Write(key.FullInstance);
    }

    public void WriteUtf16String(string value) =>
        Write(Encoding.Unicode.GetBytes(value));
}
