namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the values of a <see cref="FloorFeatureLayer"/> on one level
/// </summary>
/// <remarks>
/// The values are stored as <see cref="SizeZ"/> rows of <see cref="SizeX"/> bytes, unlike the floor grids.
/// The game's <c>get_floor_feature</c> returns a value divided by 255.
/// </remarks>
public sealed class FloorFeatureLevel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FloorFeatureLevel"/> class with every value 0
    /// </summary>
    /// <param name="sizeX">The number of tiles along the lot's local X axis, or 1 for a layer of type 2</param>
    /// <param name="sizeZ">The number of tiles along the lot's local Z axis, or 1 for a layer of type 2</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sizeX"/> or <paramref name="sizeZ"/> is not 0 through 255</exception>
    public FloorFeatureLevel(int sizeX, int sizeZ) :
        this(sizeX, sizeZ, null)
    {
    }

    internal FloorFeatureLevel(int sizeX, int sizeZ, byte[]? values)
    {
        if (sizeX is < 0 or > byte.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(sizeX), sizeX, "The size must be 0 through 255");
        if (sizeZ is < 0 or > byte.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(sizeZ), sizeZ, "The size must be 0 through 255");
        SizeX = sizeX;
        SizeZ = sizeZ;
        Values = values ?? new byte[sizeX * sizeZ];
    }

    sbyte level;

    /// <summary>
    /// Gets/sets the value of a tile
    /// </summary>
    /// <param name="x">The tile's index along the lot's local X axis, or 0 for a layer of type 2</param>
    /// <param name="z">The tile's index along the lot's local Z axis, or 0 for a layer of type 2</param>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate is outside the level</exception>
    public byte this[int x, int z]
    {
        get => Values[GetIndex(x, z)];
        set => Values[GetIndex(x, z)] = value;
    }

    /// <summary>
    /// Gets/sets the level, which cannot be −128
    /// </summary>
    /// <remarks>
    /// −4 through 4 in the lots examined, from <see cref="LotInfoSection.LowestLevel"/> through one above <see cref="LotInfoSection.HighestLevel"/>.
    /// The section ends each layer's list of levels with −128.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The level is −128</exception>
    public sbyte Level
    {
        get => level;
        set
        {
            if (value == sbyte.MinValue)
                throw new ArgumentOutOfRangeException(nameof(value), value, "The level cannot be -128, which ends the list of levels");
            level = value;
        }
    }

    /// <summary>
    /// Gets the number of values along the lot's local X axis
    /// </summary>
    public int SizeX { get; }

    /// <summary>
    /// Gets the number of values along the lot's local Z axis
    /// </summary>
    public int SizeZ { get; }

    internal byte[] Values { get; }

    int GetIndex(int x, int z)
    {
        if ((uint)x >= (uint)SizeX)
            throw new ArgumentOutOfRangeException(nameof(x), x, $"The X index must be less than {SizeX}");
        if ((uint)z >= (uint)SizeZ)
            throw new ArgumentOutOfRangeException(nameof(z), z, $"The Z index must be less than {SizeZ}");
        return z * SizeX + x;
    }
}
