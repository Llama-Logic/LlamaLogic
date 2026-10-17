namespace LlamaLogic.Architecture;

/// <summary>
/// Specifies the type of a <see cref="WallBlock"/>
/// </summary>
/// <remarks>
/// The game checks each block type against the <see cref="WallBlockSegmentKind"/> of the block's segments with a table: a <see cref="Fence"/> block allows only <see cref="WallBlockSegmentKind.Open"/>, a <see cref="Fountain"/> or <see cref="Pool"/> block only <see cref="WallBlockSegmentKind.Wall"/> and <see cref="WallBlockSegmentKind.ModularPieceWithWall"/> (otherwise BlockTypeIncompatibleWithProduct).
/// </remarks>
[SuppressMessage("Design", "CA1028: Enum Storage should be Int32", Justification = "The game stores the value as a byte.")]
public enum WallBlockType :
    byte
{
    /// <summary>
    /// A block that is not a fence, fountain or pool
    /// </summary>
    /// <remarks>
    /// In the lots examined these are rooms and free-standing wall lines, and a platform is a block of this type too: drawing one creates a block whose segments are all <see cref="WallBlockSegmentKind.Open"/> until walls are added, raised by <see cref="WallBlock.FloorHeightOffset"/> just as a room whose floor is raised (in-game tests); that the type covers nothing else is a hypothesis.
    /// </remarks>
    Standard = 0,

    /// <summary>
    /// A fence line
    /// </summary>
    /// <remarks>
    /// Placing a fence creates a block of this type (in-game test). The game allows it only at level 0 and gives it no elevation.
    /// </remarks>
    Fence = 1,

    /// <summary>
    /// A fountain
    /// </summary>
    /// <remarks>
    /// The game emits CantPlaceFountainUnderAnything for it, gives it a height of 0.375 and allows it only at level -1.
    /// </remarks>
    Fountain = 2,

    /// <summary>
    /// A pool
    /// </summary>
    /// <remarks>
    /// The game emits CantPlacePoolUnderOtherPool for it; placing a pool creates a block of this type (in-game test).
    /// </remarks>
    Pool = 3
}
