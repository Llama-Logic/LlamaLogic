namespace LlamaLogic.Architecture;

/// <summary>
/// Represents a layer of the <see cref="FloorFeaturesSection"/>: one type of floor feature, with a set of values per level
/// </summary>
/// <remarks>
/// The layout follows the game's reader.
/// </remarks>
public sealed class FloorFeatureLayer
{
    internal const byte MaximumType = 2;

    internal static (int sizeX, int sizeZ) GetLevelSize(byte type, int sizeX, int sizeZ) =>
        type == 2 ? (1, 1) : (sizeX, sizeZ);

    byte type;
    uint version = 1;

    /// <summary>
    /// Gets the levels, in the order they are stored, which is ascending in the lots examined
    /// </summary>
    /// <remarks>
    /// A level of a layer of type 0 or 1 has one value per tile (<see cref="FloorFeatureLevel.SizeX"/> and <see cref="FloorFeatureLevel.SizeZ"/> are the lot's); a level of a layer of type 2 has a single value (both are 1).
    /// </remarks>
    public Collection<FloorFeatureLevel> Levels { get; } = [];

    /// <summary>
    /// Gets/sets the type of the layer, 0 through 2
    /// </summary>
    /// <remarks>
    /// The game rejects other types as an "Invalid layer type"; the names of the three types are not in the game's code.
    /// In the lots examined the values of type 0 are 0 or 255, those of type 1 are 0, 127, 128 or 255, and those of type 2 are 0; layers of type 2 have no levels in all but 6 records.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The type is not 0 through 2</exception>
    public byte Type
    {
        get => type;
        set
        {
            if (value > MaximumType)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"The type must be 0 through {MaximumType}");
            type = value;
        }
    }

    /// <summary>
    /// Gets/sets the version of the layer
    /// </summary>
    /// <remarks>
    /// The game's reader accepts 0 and 1 and writes 1, the only version in the lots examined; both have the same layout.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The version is not 0 or 1</exception>
    public uint Version
    {
        get => version;
        set
        {
            if (value > 1)
                throw new ArgumentOutOfRangeException(nameof(value), value, "The version must be 0 or 1");
            version = value;
        }
    }
}
