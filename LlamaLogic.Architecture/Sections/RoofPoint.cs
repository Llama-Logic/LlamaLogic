namespace LlamaLogic.Architecture;

/// <summary>
/// Represents a lot-local tile position of a <see cref="Roof"/>'s footprint
/// </summary>
/// <remarks>
/// From version 10 of <see cref="RoofsSection"/> on, each point is stored as a 32-bit value: <see cref="X"/> in the low byte, then <see cref="Z"/>, then <see cref="Level"/>, then <see cref="Reserved"/> in the high byte.
/// Before version 10, a point is stored as its three low bytes.
/// </remarks>
public readonly record struct RoofPoint
{
    internal static RoofPoint FromPacked(uint value) =>
        new()
        {
            X = (byte)value,
            Z = (byte)(value >> 8),
            Level = unchecked((sbyte)(byte)(value >> 16)),
            Reserved = (byte)(value >> 24)
        };

    /// <summary>
    /// Gets/sets the signed lot level
    /// </summary>
    public sbyte Level { get; init; }

    /// <summary>
    /// Gets/sets the high byte of the stored value
    /// </summary>
    /// <remarks>
    /// The game's reader discards it and its writer writes 0; it is 0 in every lot examined.
    /// </remarks>
    public byte Reserved { get; init; }

    /// <summary>
    /// Gets/sets the tile position along the lot's local X axis
    /// </summary>
    public byte X { get; init; }

    /// <summary>
    /// Gets/sets the tile position along the lot's local Z axis
    /// </summary>
    public byte Z { get; init; }

    internal uint ToPacked() =>
        X | (uint)Z << 8 | (uint)unchecked((byte)Level) << 16 | (uint)Reserved << 24;
}
