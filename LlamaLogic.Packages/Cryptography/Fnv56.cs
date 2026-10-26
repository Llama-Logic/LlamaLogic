namespace LlamaLogic.Packages.Cryptography;

/// <summary>
/// Represents the 56-bit implementation of the Fowler–Noll–Vo hash algorithm
/// </summary>
public sealed class Fnv56 :
    Fnv64
{
    static readonly ThreadLocal<Fnv56> algorithm = new(() => new());
    const ulong mask = 0xffffffffffffff;

    /// <summary>
    /// Gets the hash value for an empty string
    /// </summary>
    public static new readonly ulong EmptyStringHash = GetHash(string.Empty);

    static ulong Convert64To56(ulong value) =>
        (value) >> 56 ^ (value & mask);

    /// <summary>
    /// Computes the hash value for the specified string
    /// </summary>
    public static new ulong GetHash(string? text)
    {
        if (algorithm.Value is not { } fnv56)
            throw new InvalidOperationException("thread local instance is null");
        Span<byte> hash = fnv56.ComputeHash(text);
        var result = Convert64To56(MemoryMarshal.Read<ulong>(hash[0..8]));
        fnv56.Reset();
        return result;
    }

    /// <summary>
    /// Sets the high bit of a 56-bit Fowler–Noll–Vo hash
    /// </summary>
    public static new ulong SetHighBit(ulong hash) =>
        hash | 0x0080000000000000;

    /// <inheritdoc/>
    public override byte[]? Hash
    {
        get
        {
            var result = new byte[sizeof(ulong)];
            var uintHash = Convert64To56((uint)hash);
            MemoryMarshal.Write(result, ref uintHash);
            return result;
        }
    }
}
