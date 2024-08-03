namespace LlamaLogic.Packages.Cryptography;

/// <summary>
/// Represents the 32-bit implementation of the Fowler–Noll–Vo hash algorithm
/// </summary>
public class Fnv32 :
    FnvHash
{
    static readonly ThreadLocal<Fnv32> algorithm = new(() => new());

    /// <summary>
    /// Gets the hash value for an empty string
    /// </summary>
    public static readonly uint EmptyStringHash = GetHash(string.Empty);

    /// <summary>
    /// Computes the hash value for the specified string
    /// </summary>
    public static uint GetHash(string? text)
    {
        if (algorithm.Value is not { } fnv32)
            throw new InvalidOperationException("thread local instance is null");
        Span<byte> hash = fnv32.ComputeHash(text);
        var result = MemoryMarshal.Read<uint>(hash[0..4]);
        fnv32.Reset();
        return result;
    }

    /// <summary>
    /// Sets the high bit of a 32-bit Fowler–Noll–Vo hash
    /// </summary>
    public static uint SetHighBit(uint hash) =>
        hash | 0x80000000;

    /// <summary>
    /// Initializes a new instance of the <see cref="Fnv32"/> class
    /// </summary>
    public Fnv32() :
        base(0x01000193, 0x811C9DC5)
    {
    }

    /// <inheritdoc/>
    public override byte[]? Hash
    {
        get
        {
            var result = new byte[sizeof(uint)];
            var uintHash = (uint)hash;
            MemoryMarshal.Write(result, ref uintHash);
            return result;
        }
    }

    /// <inheritdoc/>
    public override int HashSize =>
        sizeof(uint) * 8;
}
