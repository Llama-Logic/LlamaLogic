namespace LlamaLogic.TuningArchive;

static class Fnv
{
    public static uint Hash32(string text)
    {
        var hash = 0x811C9DC5u;
        foreach (var b in Encoding.UTF8.GetBytes(text))
        {
            hash *= 0x01000193u;
            hash ^= b;
        }
        return hash;
    }

    public static ulong Hash64(string text)
    {
        var hash = 0xCBF29CE484222325ul;
        foreach (var b in Encoding.UTF8.GetBytes(text))
        {
            hash *= 0x00000100000001B3ul;
            hash ^= b;
        }
        return hash;
    }
}