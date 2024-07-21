namespace LlamaLogic.Packages;

static class Extensions
{
    public static Span<byte> GetSpanAndAdvance(this ArrayBufferWriter<byte> writer, int count)
    {
        var span = writer.GetSpan(count);
        writer.Advance(count);
        return span;
    }

    public static void WriteIndexComponent(this Stream stream, ArrayBufferWriter<byte> index)
    {
        var position = (uint)stream.Position;
        MemoryMarshal.Write(index.GetSpanAndAdvance(sizeof(uint)), ref position);
    }
}
