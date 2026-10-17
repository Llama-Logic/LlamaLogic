namespace LlamaLogic.Architecture.UnitTests;

sealed class TestBytes
{
    readonly List<byte> bytes = [];

    public int Count =>
        bytes.Count;

    public TestBytes Add(params byte[] values)
    {
        bytes.AddRange(values);
        return this;
    }

    public TestBytes F32(float value) =>
        Add(BitConverter.GetBytes(value));

    public TestBytes I32(int value) =>
        Add(BitConverter.GetBytes(value));

    public TestBytes KeyInstanceTypeGroup(uint type, uint group, ulong instance) =>
        U64(instance).U32(type).U32(group);

    public TestBytes KeyTypeGroupInstance(uint type, uint group, ulong instance) =>
        U32(type).U32(group).U64(instance);

    public TestBytes S8(sbyte value) =>
        Add(unchecked((byte)value));

    public byte[] ToArray() =>
        [.. bytes];

    public TestBytes U8(byte value) =>
        Add(value);

    public TestBytes U16(ushort value) =>
        Add(BitConverter.GetBytes(value));

    public TestBytes U32(uint value) =>
        Add(BitConverter.GetBytes(value));

    public TestBytes U64(ulong value) =>
        Add(BitConverter.GetBytes(value));
}
