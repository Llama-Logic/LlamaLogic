namespace LlamaLogic.Architecture;

/// <summary>
/// Represents a tile of a <see cref="Pond"/> and the quarters of it the pond covers
/// </summary>
/// <remarks>
/// The game keys a cell by (<see cref="X"/>, <see cref="Z"/>, <see cref="Level"/>, <see cref="QuarterMask"/>), so one tile can appear once per quarter.
/// </remarks>
public sealed class PondCell
{
    internal const byte DefaultUnknown1 = 3;

    /// <summary>
    /// Gets/sets the tile's level
    /// </summary>
    /// <remarks>
    /// 0 in the lots examined.
    /// </remarks>
    public sbyte Level { get; set; }

    /// <summary>
    /// Gets/sets the quarters of the tile the pond covers, bit <c>n</c> standing for the <see cref="FloorQuarter"/> with value <c>n</c>
    /// </summary>
    /// <remarks>
    /// 1, 2, 4, 8 or 15 in the lots examined.
    /// </remarks>
    public byte QuarterMask { get; set; }

    /// <summary>
    /// Gets/sets a value of unknown meaning, written from version 3
    /// </summary>
    /// <remarks>
    /// The game sets it to 3 when reading an older version; it is 3 in every lot examined and was 3 in every cell of the ponds of the in-game tests.
    /// </remarks>
    public byte Unknown1 { get; set; } = DefaultUnknown1;

    /// <summary>
    /// Gets/sets the tile's index along the lot's local X axis
    /// </summary>
    public byte X { get; set; }

    /// <summary>
    /// Gets/sets the tile's index along the lot's local Z axis
    /// </summary>
    public byte Z { get; set; }
}
