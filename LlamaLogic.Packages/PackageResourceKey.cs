namespace LlamaLogic.Packages;

/// <summary>
/// Represents the unique key of a resource within a package
/// </summary>
/// <remarks>
/// Instantiates a new package resource key
/// </remarks>
public
#if IS_NET_7_0_OR_GREATER
partial
#endif
struct PackageResourceKey(PackageResourceType type, uint group, ulong instance) :
    IEquatable<PackageResourceKey>
#if IS_NET_7_0_OR_GREATER
    , IParsable<PackageResourceKey>
#endif
{
#if IS_NET_7_0_OR_GREATER
    [GeneratedRegex(@"^(?<type>[\da-f]{8})-(?<group>[\da-f]{8})-(?<instance>[\da-f]{16})$", RegexOptions.IgnoreCase)]
    public static partial Regex GetPackageResourceKeyRegex();
#endif

    /// <inheritdoc/>
    public static PackageResourceKey Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var key))
            return key;
        throw new FormatException($"Unable to parse '{s}' as {nameof(PackageResourceKey)}.");
    }

    /// <inheritdoc/>
    public static bool TryParse(string? s, IFormatProvider? provider, out PackageResourceKey result)
    {
#if IS_NET_6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(s);
#else
        if (s is null)
            throw new ArgumentNullException(nameof(s));
#endif
#if IS_NET_7_0_OR_GREATER
        var match = GetPackageResourceKeyRegex().Match(s);
#else
        var match = Regex.Match(s, @"^(?<type>[\da-f]{8})-(?<group>[\da-f]{8})-(?<instance>[\da-f]{16})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
#endif
        if (match.Success)
        {
            result = new PackageResourceKey((PackageResourceType)uint.Parse(match.Groups["type"].Value, NumberStyles.HexNumber), uint.Parse(match.Groups["group"].Value, NumberStyles.HexNumber), ulong.Parse(match.Groups["instance"].Value, NumberStyles.HexNumber));
            return true;
        }
        result = default;
        return false;
    }

    /// <inheritdoc/>
    public static bool operator ==(PackageResourceKey left, PackageResourceKey right) =>
        left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(PackageResourceKey left, PackageResourceKey right) =>
        !(left == right);

    /// <summary>
    /// Gets the full TGI or TGI ID (type, group, and instance in hex format) of the resource key
    /// </summary>
    public readonly string FullTgi =>
        $"{(uint)Type:x8}-{GroupHex}-{InstanceHex}";

    /// <summary>
    /// A value used to group related resources together
    /// </summary>
    /// <remarks>
    /// In many cases, this value can be 0, especially for custom content.
    /// However, Maxis content and some specific modding scenarios might use different group values to organize resources.
    /// Group values help in categorizing resources that belong to a specific set or context.
    /// </remarks>
    public uint Group = group;

    /// <summary>
    /// Gets the group in hex format
    /// </summary>
    public readonly string GroupHex =>
        Group.ToString("x8");

    internal readonly uint HighOrderInstance =>
        (uint)(Instance >> 32);

    /// <summary>
    /// A value that uniquely identifies an individual resource within a given type and group
    /// </summary>
    public readonly ulong Instance = instance;

    /// <summary>
    /// Gets the instance in hex format
    /// </summary>
    public readonly string InstanceHex =>
        Instance.ToString("x16");

    /// <summary>
    /// The type of resource indicated by the key
    /// </summary>
    public PackageResourceType Type = type;

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj) =>
        obj is PackageResourceKey key && Equals(key);

    /// <inheritdoc/>
    public readonly bool Equals(PackageResourceKey other) =>
        Type.Equals(other.Type) && Group.Equals(other.Group) && Instance.Equals(other.Instance);

    /// <inheritdoc/>
    public override readonly int GetHashCode() =>
        HashCode.Combine(Type, Group, Instance);

    /// <inheritdoc/>
    public override readonly string ToString() =>
        FullTgi;

    internal void WriteIndexComponent(ArrayBufferWriter<byte> index, bool writeTypes, bool writeGroups, bool writeHighOrderInstances)
    {
        if (writeTypes)
            MemoryMarshal.Write(index.GetSpanAndAdvance(sizeof(PackageResourceType)), ref Type);
        if (writeGroups)
            MemoryMarshal.Write(index.GetSpanAndAdvance(sizeof(uint)), ref Group);
        if (writeHighOrderInstances)
        {
            var highOrderInstance = HighOrderInstance;
            MemoryMarshal.Write(index.GetSpanAndAdvance(sizeof(uint)), ref highOrderInstance);
        }
        var lowOrderInstance = (uint)(Instance & 0xffffffff);
        MemoryMarshal.Write(index.GetSpanAndAdvance(sizeof(uint)), ref lowOrderInstance);
    }
}
