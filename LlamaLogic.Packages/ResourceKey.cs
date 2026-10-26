namespace LlamaLogic.Packages;

/// <summary>
/// Represents the unique key of a resource within a <see cref="DataBasePackedFile"/>
/// </summary>
public
#if IS_NET_7_0_OR_GREATER
partial
#endif
struct ResourceKey(ResourceType type, uint group, ulong fullInstance) :
    IEquatable<ResourceKey>
#if IS_NET_7_0_OR_GREATER
    , IParsable<ResourceKey>
#endif
{
    /// <summary>
    /// Represents the empty <see cref="ResourceKey"/>
    /// </summary>
    public static readonly ResourceKey Empty = new(0, 0, 0);

#if IS_NET_7_0_OR_GREATER
    [GeneratedRegex(@"^(?<type>[\da-f]{8}):(?<group>[\da-f]{8}):(?<instanceEx>[\da-f]{8})(?<instance>[\da-f]{8})$|^(?<type>[\da-f]{8})-(?<group>[\da-f]{8})-(?<instanceEx>[\da-f]{8})(?<instance>[\da-f]{8})$", RegexOptions.IgnoreCase)]
    public static partial Regex GetPackageResourceKeyRegex();
#endif

    /// <summary>
    /// Parses a string into a <see cref="ResourceKey"/>
    /// </summary>
    /// <param name="s">The string to parse</param>
    /// <exception cref="FormatException"><paramref name="s"/> is not in the correct format</exception>
    public static ResourceKey Parse(string s) =>
        Parse(s, null);

    /// <summary>
    /// Parses a string into a <see cref="ResourceKey"/>
    /// </summary>
    /// <param name="s">The string to parse</param>
    /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s"/></param>
    /// <exception cref="FormatException"><paramref name="s"/> is not in the correct format</exception>
    public static ResourceKey Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var key))
            return key;
        throw new FormatException($"Unable to parse '{s}' as {nameof(ResourceKey)}.");
    }

    /// <summary>
    /// Tries to parse a string into a <see cref="ResourceKey"/>
    /// </summary>
    /// <param name="s">The string to parse</param>
    /// <param name="result">When this method returns, contains the result of successfully parsing <paramref name="s"/> or an undefined value on failure</param>
    public static bool TryParse(string? s, out ResourceKey result) =>
        TryParse(s, null, out result);

    /// <summary>
    /// Tries to parse a string into a <see cref="ResourceKey"/>
    /// </summary>
    /// <param name="s">The string to parse</param>
    /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s"/></param>
    /// <param name="result">When this method returns, contains the result of successfully parsing <paramref name="s"/> or an undefined value on failure</param>
    public static bool TryParse(string? s, IFormatProvider? provider, out ResourceKey result)
    {
        ArgumentNullException.ThrowIfNull(s);
#if IS_NET_7_0_OR_GREATER
        var match = GetPackageResourceKeyRegex().Match(s);
#else
        var match = Regex.Match(s, @"^(?<type>[\da-f]{8}):(?<group>[\da-f]{8}):(?<instanceEx>[\da-f]{8})(?<instance>[\da-f]{8})$|^(?<type>[\da-f]{8})-(?<group>[\da-f]{8})-(?<instanceEx>[\da-f]{8})(?<instance>[\da-f]{8})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
#endif
        if (match.Success)
        {
            result = new ResourceKey((ResourceType)uint.Parse(match.Groups["type"].Value, NumberStyles.HexNumber), uint.Parse(match.Groups["group"].Value, NumberStyles.HexNumber), ((ulong)uint.Parse(match.Groups["instanceEx"].Value, NumberStyles.HexNumber)) << 32 | uint.Parse(match.Groups["instance"].Value, NumberStyles.HexNumber));
            return true;
        }
        result = default;
        return false;
    }

    /// <inheritdoc/>
    public static bool operator ==(ResourceKey left, ResourceKey right) =>
        left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(ResourceKey left, ResourceKey right) =>
        !(left == right);

    /// <summary>
    /// Converts a string into a <see cref="ResourceKey"/>
    /// </summary>
    /// <param name="s">The string to convert</param>
    [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "For convenience in Python.")]
    public static implicit operator ResourceKey(string s) =>
        Parse(s);

    /// <summary>
    /// Gets the full TGI or TGI ID (type, group, and instance in hex format) of the resource key
    /// </summary>
    public readonly string FullTgi =>
        $"{TypeHex}:{GroupHex}:{FullInstanceHex}";

    /// <summary>
    /// A value used to group related resources together
    /// </summary>
    /// <remarks>
    /// In many cases, this value can be 0, especially for custom content.
    /// However, Maxis content and some specific modding scenarios might use different group values to organize resources.
    /// Group values help in categorizing resources that belong to a specific set or context.
    /// </remarks>
    public readonly uint Group = group;

    /// <summary>
    /// Gets the hex representation of <see cref="Group"/>
    /// </summary>
    public readonly string GroupHex =>
        Group.ToString("x8");

    /// <summary>
    /// A value that uniquely identifies an individual resource within a given type and group
    /// </summary>
    public readonly ulong FullInstance = fullInstance;

    /// <summary>
    /// Gets the hex representation of <see cref="FullInstance"/>
    /// </summary>
    public readonly string FullInstanceHex =>
        FullInstance.ToString("x16");

    /// <summary>
    /// Gets the low order bits of <see cref="FullInstance"/>
    /// </summary>
    public readonly uint Instance =>
        (uint)(FullInstance & 0xffffffff);

    /// <summary>
    /// Gets the high order bits of <see cref="FullInstance"/>
    /// </summary>
    public readonly uint InstanceEx =>
        (uint)(FullInstance >> 32);

    /// <summary>
    /// Gets the hex representation of <see cref="Instance"/>
    /// </summary>
    public readonly string InstanceHex =>
        Instance.ToString("x8");

    /// <summary>
    /// Gets the hex representation of <see cref="InstanceEx"/>
    /// </summary>
    public readonly string InstanceExHex =>
        InstanceEx.ToString("x8");

    /// <summary>
    /// The type of resource indicated by the key
    /// </summary>
    public readonly ResourceType Type = type;

    /// <summary>
    /// Gets the hex representation of <see cref="Type"/>
    /// </summary>
    public readonly string TypeHex =>
        ((uint)Type).ToString("x8");

    /// <summary>
    /// Deconstructs the key into its components
    /// </summary>
    /// <param name="type">The <see cref="Type"/></param>
    /// <param name="group">The <see cref="Group"/></param>
    /// <param name="fullInstance">The <see cref="FullInstance"/></param>
    public readonly void Deconstruct(out ResourceType type, out uint group, out ulong fullInstance)
    {
        type = Type;
        group = Group;
        fullInstance = FullInstance;
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj) =>
        obj is ResourceKey key && Equals(key);

    /// <inheritdoc/>
    public readonly bool Equals(ResourceKey other) =>
        Type.Equals(other.Type) && Group.Equals(other.Group) && FullInstance.Equals(other.FullInstance);

    /// <inheritdoc/>
    public override readonly int GetHashCode() =>
        HashCode.Combine(Type, Group, FullInstance);

    /// <inheritdoc/>
    public override readonly string ToString() =>
        FullTgi;
}
