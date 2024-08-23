namespace LlamaLogic.Packages.Models;

/// <summary>
/// Represents a <see cref="ResourceType.StringTable"/> resource (🔒)
/// </summary>
public class StringTableModel :
    Model,
    IModel<StringTableModel>
{
    static readonly Memory<byte> expectedFileIdentifier = "STBL"u8.ToArray();
    const ushort expectedVersion = 5;
    static readonly ImmutableHashSet<ResourceType> supportedTypes =
    [
        ResourceType.StringTable
    ];

    /// <inheritdoc/>
    public static new ISet<ResourceType> SupportedTypes =>
        supportedTypes;

    /// <inheritdoc/>
    public static StringTableModel Decode(ReadOnlyMemory<byte> data)
    {
        var dataSpan = data.Span;
        var mnFileIdentifier = dataSpan[0..4];
        if (!mnFileIdentifier.SequenceEqual(expectedFileIdentifier.Span))
            throw new NotSupportedException($"File identifier \"{Encoding.ASCII.GetString(mnFileIdentifier)}\" is invalid");
        var readPosition = 4U;
        var mnVersion = dataSpan.ReadAndAdvancePosition<ushort>(ref readPosition);
        if (mnVersion != expectedVersion)
            throw new NotSupportedException($"Version {mnVersion} is not supported, expected {expectedVersion}");
        ++readPosition; // mbCompressed
        var mnNumEntries = dataSpan.ReadAndAdvancePosition<ulong>(ref readPosition);
        readPosition += 6; // mReserved[2] & mnStringLength
        var stringTableModel = new StringTableModel();
        var entries = stringTableModel.entries;
        for (ulong i = 0; i < mnNumEntries; ++i)
        {
            var mnKeyHash = dataSpan.ReadAndAdvancePosition<uint>(ref readPosition);
            ++readPosition; // mnFlags
            var mnLength = dataSpan.ReadAndAdvancePosition<ushort>(ref readPosition);
            var @string = mnLength > 0
                ? Encoding.UTF8.GetString(dataSpan[(int)readPosition..(int)(readPosition += mnLength)])
                : string.Empty;
            entries.Add(mnKeyHash, @string);
        }
        return stringTableModel;
    }

    /// <inheritdoc/>
    public static new string? GetName(Stream stream) =>
        null;

    /// <inheritdoc/>
    public static new Task<string?> GetNameAsync(Stream stream, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    readonly Dictionary<uint, string> entries = [];
    readonly AsyncLock resourceLock = new();

    /// <summary>
    /// Gets the number of entries in the string table
    /// </summary>
    public int Count
    {
        get
        {
            using var heldResourceLock = resourceLock.Lock();
            return entries.Count;
        }
    }

    /// <summary>
    /// Gets the key hashes in the string table
    /// </summary>
    public IReadOnlyList<uint> KeyHashes
    {
        get
        {
            using var heldResourceLock = resourceLock.Lock();
            return [..entries.Keys];
        }
    }

    /// <inheritdoc/>
    public override string? ResourceName =>
        null;

    /// <summary>
    /// Gets/sets the string associated with a <paramref name="keyHash"/>
    /// </summary>
    public string this[uint keyHash]
    {
        get => Get(keyHash);
        set => Set(keyHash, value);
    }

    /// <summary>
    /// Adds <paramref name="string"/> to the string table, returning its key hash
    /// </summary>
    /// <exception cref="ArgumentException">The key hash generated for <paramref name="string"/> already existed in the string table</exception>
    public uint AddNew(string @string)
    {
        ArgumentNullException.ThrowIfNull(@string);
        var keyHash = Fnv32.GetHash(@string);
        using var heldResourceLock = resourceLock.Lock();
        ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(entries, keyHash, out var exists);
        if (exists)
            throw new ArgumentException($"Duplicate keyHash 0x{keyHash:x8} already exists in the string table; to add your new entry with a different key, use the {nameof(Set)} method");
        entry = @string;
        return keyHash;
    }

    /// <summary>
    /// Deletes the string associated with a <paramref name="keyHash"/>, returning <see langword="true"/> if the key hash existed in the string table
    /// </summary>
    public bool Delete(uint keyHash)
    {
        using var heldResourceLock = resourceLock.Lock();
        return entries.Remove(keyHash);
    }

    /// <inheritdoc/>
    public override ReadOnlyMemory<byte> Encode()
    {
        var writer = new ArrayBufferWriter<byte>(21 + entries.Sum(entry => 7 + Encoding.UTF8.GetByteCount(entry.Value)));
        writer.Write(expectedFileIdentifier.Span);
        var mnVersion = expectedVersion;
        writer.Write(ref mnVersion);
        writer.Advance(1); // mbCompressed
        using var heldResourceLock = resourceLock.Lock();
        var mnNumEntries = (ulong)entries.Count;
        writer.Write(ref mnNumEntries);
        writer.Advance(2); // mReserved[2]
        var mnStringLength = (uint)entries.Values.Sum(entry => entry.Length + 1);
        writer.Write(ref mnStringLength);
        foreach (var (key, @string) in entries)
        {
            var mnKeyHash = key;
            writer.Write(ref mnKeyHash);
            writer.Advance(1); // mnFlags
            var mnLength = (ushort)@string.Length;
            writer.Write(ref mnLength);
            if (mnLength > 0)
                writer.Write(Encoding.UTF8.GetBytes(@string));
        }
        return writer.WrittenMemory;
    }

    /// <summary>
    /// Gets the string associated with a <paramref name="keyHash"/>
    /// </summary>
    public string Get(uint keyHash)
    {
        using var heldResourceLock = resourceLock.Lock();
        ref var entry = ref CollectionsMarshal.GetValueRefOrNullRef(entries, keyHash);
        if (!Unsafe.IsNullRef(ref entry))
            return entry;
        return string.Empty;
    }

    /// <summary>
    /// Sets the <paramref name="string"/> associated with a <paramref name="keyHash"/>, returning <see langword="true"/> if the key hash did not already exist in the string table
    /// </summary>
    public bool Set(uint keyHash, string @string)
    {
        using var heldResourceLock = resourceLock.Lock();
        ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(entries, keyHash, out var exists);
        entry = @string;
        return !exists;
    }
}
