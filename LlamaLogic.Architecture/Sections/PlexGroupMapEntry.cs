namespace LlamaLogic.Architecture;

/// <summary>
/// Represents an entry of a <see cref="PlexGroupMap"/>
/// </summary>
public sealed class PlexGroupMapEntry
{
    /// <summary>
    /// Gets/sets the <see cref="PlexGroup.Id"/> of the group
    /// </summary>
    /// <remarks>
    /// It is the id of a group of the bundle's <see cref="PlexGroupsSection"/> in every lot examined.
    /// </remarks>
    public uint GroupId { get; set; }

    /// <summary>
    /// Gets/sets the inner key of the map entry
    /// </summary>
    /// <remarks>
    /// Hypothesis: an index of exterior variants, which an operation of the game swaps.
    /// </remarks>
    public sbyte MapKey { get; set; }
}
