namespace LlamaLogic.Packages.Cryptography;

/// <summary>
/// Represents the 24-bit implementation of the Fowler–Noll–Vo hash algorithm
/// </summary>
public class Fnv24 :
    Fnv32
{
    static readonly ThreadLocal<Fnv24> algorithm = new(() => new());
    const uint mask = 0xffffff;

    /// <summary>
    /// Gets the hash value for an empty string
    /// </summary>
    public static new readonly uint EmptyStringHash = GetHash(string.Empty);

    static uint Convert32To24(uint value) =>
        (value) >> 24 ^ (value & mask);

    /// <summary>
    /// Computes the hash value for the specified string
    /// </summary>
    public static new uint GetHash(string? text)
    {
        if (algorithm.Value is not { } fnv24)
            throw new InvalidOperationException("thread local instance is null");
        Span<byte> hash = algorithm.Value!.ComputeHash(text);
        var result = Convert32To24(MemoryMarshal.Read<uint>(hash[0..4]));
        fnv24.Reset();
        return result;
    }

    /// <summary>
    /// Sets the high bit of a 24-bit Fowler–Noll–Vo hash
    /// </summary>
    public static new uint SetHighBit(uint hash) =>
        hash | 0x00800000;

    /// <inheritdoc/>
    public override byte[]? Hash
    {
        get
        {
            var result = new byte[sizeof(uint)];
            var uintHash = Convert32To24((uint)hash);
            MemoryMarshal.Write(result, ref uintHash);
            return result;
        }
    }
}
