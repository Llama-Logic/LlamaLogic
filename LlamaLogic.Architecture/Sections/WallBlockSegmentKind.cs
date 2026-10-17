namespace LlamaLogic.Architecture;

/// <summary>
/// Specifies the kind of a <see cref="WallBlockSegment"/>
/// </summary>
/// <remarks>
/// The game treats a block as enclosed when none of the segments of its outer loop is <see cref="Open"/> or <see cref="ModularPiece"/>, emits CantPlacePoolUnderWall for a <see cref="Wall"/> segment, and requires <see cref="ModularPiece"/> or <see cref="ModularPieceWithWall"/> on a diagonal segment flanked by 45° segments of the same kind, otherwise InvalidBlockModularPieceConfiguration.
/// That such segments form rounded corners is a hypothesis.
/// Before block version 27 the game maps the stored values 0 through 3 to 0, 1, 2 and 1 with a table.
/// </remarks>
[SuppressMessage("Design", "CA1028: Enum Storage should be Int32", Justification = "The game stores the value as a byte.")]
public enum WallBlockSegmentKind :
    byte
{
    /// <summary>
    /// A segment with a wall
    /// </summary>
    Wall = 0,

    /// <summary>
    /// A segment without a wall
    /// </summary>
    Open = 1,

    /// <summary>
    /// A segment of a modular piece without a wall
    /// </summary>
    ModularPiece = 2,

    /// <summary>
    /// A segment of a modular piece with a wall
    /// </summary>
    ModularPieceWithWall = 3
}
