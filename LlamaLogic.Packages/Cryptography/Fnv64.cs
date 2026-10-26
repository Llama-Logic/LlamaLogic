namespace LlamaLogic.Packages.Cryptography;

/// <summary>
/// Represents the 64-bit implementation of the Fowler–Noll–Vo hash algorithm
/// </summary>
public class Fnv64 :
    FnvHash
{
    static readonly ThreadLocal<Fnv64> algorithm = new(() => new());

    /// <summary>
    /// Gets the hash value for an empty string
    /// </summary>
    public static readonly ulong EmptyStringHash = GetHash(string.Empty);

    /// <summary>
    /// Computes the hash value for the specified string
    /// </summary>
    public static ulong GetHash(string? text)
    {
        if (algorithm.Value is not { } fnv64)
            throw new InvalidOperationException("thread local instance is null");
        Span<byte> hash = fnv64.ComputeHash(text);
        var result = MemoryMarshal.Read<ulong>(hash[0..8]);
        fnv64.Reset();
        return result;
    }

    /// <summary>
    /// Sets the high bit of a 64-bit Fowler–Noll–Vo hash
    /// </summary>
    public static ulong SetHighBit(ulong hash) =>
        hash | 0x8000000000000000;

    /// <summary>
    /// Initializes a new instance of the <see cref="Fnv64"/> class
    /// </summary>
    public Fnv64() :
        base(0x00000100000001B3, 0xCBF29CE484222325)
    {
    }

    /// <inheritdoc/>
    public override byte[]? Hash
    {
        get
        {
            var result = new byte[sizeof(ulong)];
            MemoryMarshal.Write(result, ref hash);
            return result;
        }
    }

    /// <inheritdoc/>
    public override int HashSize =>
        sizeof(ulong) * 8;
}
