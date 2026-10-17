namespace LlamaLogic.Architecture;

/// <summary>
/// Represents an entry of the <see cref="FloorKeysSection"/>: a floor pattern key and the id the floor id grids use for it
/// </summary>
public sealed class FloorKeyEntry
{
    /// <summary>
    /// Gets/sets the id the <see cref="FloorIdGridSection"/> grids store for <see cref="Key"/>
    /// </summary>
    /// <remarks>
    /// Ids are unique within the section and never 0; the game allocates the smallest unused id of 1 or more.
    /// </remarks>
    public uint Id { get; set; }

    /// <summary>
    /// Gets/sets the key, a CTProductFloorPattern (0xB4F762C9)
    /// </summary>
    /// <remarks>
    /// It is the null key in 4 of the entries examined.
    /// </remarks>
    public ResourceKey Key { get; set; }

    /// <summary>
    /// Gets/sets the number of references to the entry, counted in tile quarters
    /// </summary>
    /// <remarks>
    /// The game decrements it as quarters stop using the id and frees the entry at 0; it is never 0 in the lots examined.
    /// It equals the number of quarters holding <see cref="Id"/> across the seven <see cref="FloorIdGridSection"/> grids in 18,597 of the 18,773 entries examined in lots whose grids can be read, and is larger in the rest, which suggests other sections also hold references.
    /// This library does not recompute it.
    /// </remarks>
    public uint ReferenceCount { get; set; }
}
