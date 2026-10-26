namespace LlamaLogic.Packages;

static class BinaryUtilities
{
    static readonly ConcurrentDictionary<Type, int> typeSizes = new();

    public const byte FalseByte = 0;
    public const int NullOffset = int.MinValue;
    public const byte TrueByte = 1;

    public static Memory<byte> GetMemoryAndAdvance(this ArrayBufferWriter<byte> writer, int count)
    {
        var span = writer.GetMemory(count)[..count];
        writer.Advance(count);
        return span;
    }

    public static Span<byte> GetSpanAndAdvance(this ArrayBufferWriter<byte> writer, int count)
    {
        var span = writer.GetSpan(count)[..count];
        writer.Advance(count);
        return span;
    }

    public static Span<byte> GetSpanAndAdvance<T>(this ArrayBufferWriter<byte> writer)
        where T : struct =>
        GetSpanAndAdvance(writer, typeSizes.GetOrAdd(typeof(T), TypeSizeValueFactory));

    static T Read<T>(this ReadOnlyMemory<byte> readOnlyMemory, uint position, out int size)
        where T : struct
    {
        size = typeSizes.GetOrAdd(typeof(T), TypeSizeValueFactory);
        var iPosition = (int)position;
        return MemoryMarshal.Read<T>(readOnlyMemory[iPosition..(iPosition + size)].Span);
    }

    static T Read<T>(this ReadOnlySpan<byte> readOnlySpan, uint position, out int size)
        where T : struct
    {
        size = typeSizes.GetOrAdd(typeof(T), TypeSizeValueFactory);
        var iPosition = (int)position;
        return MemoryMarshal.Read<T>(readOnlySpan[iPosition..(iPosition + size)]);
    }

    public static T Read<T>(this ReadOnlyMemory<byte> readOnlyMemory, uint position)
        where T : struct =>
        Read<T>(readOnlyMemory, position, out _);

    public static T Read<T>(this ReadOnlySpan<byte> readOnlySpan, uint position)
        where T : struct =>
        Read<T>(readOnlySpan, position, out _);

    public static T ReadAndAdvancePosition<T>(this ReadOnlyMemory<byte> readOnlyMemory, ref uint position)
        where T : struct
    {
        var result = Read<T>(readOnlyMemory, position, out var size);
        position += (uint)size;
        return result;
    }

    public static T ReadAndAdvancePosition<T>(this ReadOnlySpan<byte> readOnlySpan, ref uint position)
        where T : struct
    {
        var result = Read<T>(readOnlySpan, position, out var size);
        position += (uint)size;
        return result;
    }

    public static T ReadAndAdvancePosition<T>(this ReadOnlySpan<byte> readOnlySpan, ref int position)
        where T : struct
    {
        var size = typeSizes.GetOrAdd(typeof(T), TypeSizeValueFactory);
        var result = MemoryMarshal.Read<T>(readOnlySpan[position..(position + size)]);
        position += size;
        return result;
    }

    public static int TypeSizeValueFactory(Type type)
    {
        if (type.IsEnum)
            type = type.GetEnumUnderlyingType();
        return Marshal.SizeOf(type);
    }

    public static void Write<T>(this ArrayBufferWriter<byte> writer, ref T value)
        where T : struct =>
        MemoryMarshal.Write(GetSpanAndAdvance<T>(writer), ref value);
}
