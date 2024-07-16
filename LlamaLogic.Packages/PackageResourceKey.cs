namespace LlamaLogic.Packages;

/// <summary>
/// Represents the unique key of a resource within a package
/// </summary>
[CLSCompliant(false)]
public
#if IS_NET_7_0_OR_GREATER
partial
#endif
struct PackageResourceKey :
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
    /// Instantiates a new package resource key.
    /// </summary>
    public PackageResourceKey(PackageResourceType type, uint group, ulong instance)
    {
        Type = type;
        Group = group;
        Instance = instance;
    }

    /// <summary>
    /// A value used to group related resources together
    /// </summary>
    /// <remarks>
    /// In many cases, this value can be 0, especially for custom content.
    /// However, Maxis content and some specific modding scenarios might use different group values to organize resources.
    /// Group values help in categorizing resources that belong to a specific set or context.
    /// </remarks>
    public uint Group;

    /// <summary>
    /// A value that uniquely identifies an individual resource within a given type and group
    /// </summary>
    public ulong Instance;

    /// <summary>
    /// The type of resource indicated by the key
    /// </summary>
    public PackageResourceType Type;

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is PackageResourceKey key && Equals(key);

    /// <inheritdoc/>
    public bool Equals(PackageResourceKey other) =>
        Type.Equals(other.Type) && Group.Equals(other.Group) && Instance.Equals(other.Instance);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(Type, Group, Instance);

    /// <inheritdoc/>
    public override string ToString() =>
        $"{Type:x}-{Group:x}-{Instance:x}";
}
