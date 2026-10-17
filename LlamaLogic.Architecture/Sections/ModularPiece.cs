namespace LlamaLogic.Architecture;

/// <summary>
/// Represents a modular piece of the <see cref="ModularPiecesSection"/>
/// </summary>
/// <remarks>
/// The key types below are those of every nonzero key in the lots examined.
/// </remarks>
public sealed class ModularPiece
{
    /// <summary>
    /// Gets/sets the key of the piece's ceiling rail, a CTProductCeilingRail (0x3F0C529A), or the null key
    /// </summary>
    public ResourceKey CeilingRailKey { get; set; }

    /// <summary>
    /// Gets/sets the key of the piece's fence, a CTProductFence (0x0418FE2A) or CTProductWall (0x9151E6BC), or the null key
    /// </summary>
    /// <remarks>
    /// The game checks its type against CTProductWall.
    /// </remarks>
    public ResourceKey FenceKey { get; set; }

    /// <summary>
    /// Gets/sets the first wall pattern key of the piece, a CTProductWallPattern (0xD5F0F921), or the null key, written from version 5
    /// </summary>
    /// <remarks>
    /// Both wall pattern keys are set or both are null in every lot examined; they differ in 218 of the 278 pieces that have them.
    /// </remarks>
    public ResourceKey FirstWallPatternKey { get; set; }

    /// <summary>
    /// Gets/sets the key of the piece's floor pattern, a CTProductFloorPattern (0xB4F762C9), or the null key
    /// </summary>
    /// <remarks>
    /// When it reads a version before 3, the game replaces a key of any other type with a default.
    /// </remarks>
    public ResourceKey FloorPatternKey { get; set; }

    /// <summary>
    /// Gets/sets the piece's level, the high byte of <see cref="Position"/>
    /// </summary>
    public sbyte Level
    {
        get => unchecked((sbyte)(Position >> 24));
        set => Position = (Position & 0x00ffffffU) | (uint)(byte)value << 24;
    }

    /// <summary>
    /// Gets/sets the key of the piece, a CTProductModularPiece (0xA0451CBD)
    /// </summary>
    public ResourceKey ModularPieceKey { get; set; }

    /// <summary>
    /// Gets/sets the piece's position, which the game reads as one 32-bit value and also uses as the piece's key
    /// </summary>
    /// <remarks>
    /// The low byte is 0 in the lots examined; the other three are <see cref="X"/>, <see cref="Z"/> and <see cref="Level"/>, from low to high.
    /// </remarks>
    public uint Position { get; set; }

    /// <summary>
    /// Gets/sets the second wall pattern key of the piece, a CTProductWallPattern (0xD5F0F921), or the null key, written from version 5
    /// </summary>
    public ResourceKey SecondWallPatternKey { get; set; }

    /// <summary>
    /// Gets/sets a key of unknown role
    /// </summary>
    /// <remarks>
    /// The null key in every lot examined.
    /// </remarks>
    public ResourceKey Unknown1 { get; set; }

    /// <summary>
    /// Gets/sets a value of unknown meaning
    /// </summary>
    /// <remarks>
    /// 0, 2, 4 or 6 in the lots examined; the game passes it to its footprint routine.
    /// Hypothesis: the piece's orientation, in eighths of a turn.
    /// </remarks>
    public byte Unknown2 { get; set; }

    /// <summary>
    /// Gets/sets a value of unknown meaning, written from version 6
    /// </summary>
    /// <remarks>
    /// 0 in 1,254 of the 1,317 version 6 pieces examined, otherwise 8 through 92.
    /// </remarks>
    public ushort Unknown3 { get; set; }

    /// <summary>
    /// Gets/sets the piece's position along the lot's local X axis, in half tiles: the second byte of <see cref="Position"/>
    /// </summary>
    /// <remarks>
    /// The game multiplies it, as a signed byte, by 0.5.
    /// </remarks>
    public sbyte X
    {
        get => unchecked((sbyte)(Position >> 8));
        set => Position = (Position & 0xffff00ffU) | (uint)(byte)value << 8;
    }

    /// <summary>
    /// Gets/sets the piece's position along the lot's local Z axis, in half tiles: the third byte of <see cref="Position"/>
    /// </summary>
    /// <remarks>
    /// The game multiplies it, as a signed byte, by 0.5.
    /// </remarks>
    public sbyte Z
    {
        get => unchecked((sbyte)(Position >> 16));
        set => Position = (Position & 0xff00ffffU) | (uint)(byte)value << 16;
    }
}
