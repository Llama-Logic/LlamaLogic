namespace LlamaLogic.Packages.Cryptography;

/// <summary>
/// Represents the base class from which this library's implementations of the Fowler–Noll–Vo hash algorithms derive
/// </summary>
public abstract class FnvHash(ulong prime, ulong offset) :
    HashAlgorithm
{
    readonly ulong offset = offset;

    /// <summary>
    /// The hash aggregate
    /// </summary>
    protected ulong hash = offset;

    /// <summary>
    /// Computes the hash value for the specified string
    /// </summary>
    [SuppressMessage("Globalization", "CA1308: Normalize strings to uppercase", Justification = "It's not our fault Maxis did this wrong, Code Analyzer.")]
    public byte[] ComputeHash(string? value) =>
        ComputeHash(Encoding.ASCII.GetBytes((value ?? string.Empty).ToLowerInvariant()));

    /// <inheritdoc/>
    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
#if IS_NET_6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(array);
#else
        if (array is null)
            throw new ArgumentNullException(nameof(array));
#endif
        var uBound = ibStart + cbSize;
        for (var i = ibStart; i < uBound; ++i)
        {
            hash *= prime;
            hash ^= array[i];
        }
    }

    /// <inheritdoc/>
    protected override byte[] HashFinal()
    {
        var result = new byte[sizeof(ulong)];
        MemoryMarshal.Write(result, ref hash);
        HashValue = result;
        return result;
    }

    /// <inheritdoc/>
    public override void Initialize()
    {
    }

    internal void Reset()
    {
        hash = offset;
        HashValue = [];
    }
}
