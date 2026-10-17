namespace LlamaLogic.Architecture;

/// <summary>
/// The four triangular quarters of a tile, named by the side of the tile each one touches, in the order the game stores them
/// </summary>
/// <remarks>
/// The game places each quarter at a centroid offset of a third of a tile from the tile's center toward its side.
/// </remarks>
[SuppressMessage("Design", "CA1028: Enum Storage should be Int32", Justification = "A quarter is stored as a 2-bit index.")]
public enum FloorQuarter :
    byte
{
    /// <summary>
    /// The quarter on the tile's side toward negative Z
    /// </summary>
    NegativeZ = 0,

    /// <summary>
    /// The quarter on the tile's side toward positive X
    /// </summary>
    PositiveX = 1,

    /// <summary>
    /// The quarter on the tile's side toward positive Z
    /// </summary>
    PositiveZ = 2,

    /// <summary>
    /// The quarter on the tile's side toward negative X
    /// </summary>
    NegativeX = 3
}
