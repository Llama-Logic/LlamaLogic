namespace LlamaLogic.Architecture;

/// <summary>
/// Represents one level of an <see cref="UnknownC9106475Entry"/>
/// </summary>
public sealed class UnknownC9106475Level
{
    /// <summary>
    /// Gets the number of keys of a level in version 3
    /// </summary>
    public const int KeyCount = 6;

    /// <summary>
    /// Gets the number of keys of a level before version 3
    /// </summary>
    public const int LegacyKeyCount = 3;

    /// <summary>
    /// Gets the keys, of which a version 3 section needs six and an older one three
    /// </summary>
    /// <remarks>
    /// The section stores each key instance first, then type, then group.
    /// Encoding fails when the number of keys is not the number the version stores.
    /// </remarks>
    public Collection<ResourceKey> Keys { get; } = [];

    /// <summary>
    /// Gets/sets the lot level
    /// </summary>
    /// <remarks>
    /// The section stores it as a 32-bit integer; the game keeps only a signed byte.
    /// </remarks>
    public sbyte Level { get; set; }
}
