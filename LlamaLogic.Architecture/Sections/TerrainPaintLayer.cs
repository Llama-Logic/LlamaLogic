namespace LlamaLogic.Architecture;

/// <summary>
/// Represents a layer of the <see cref="TerrainPaintSection"/>: a terrain paint and the mask of where it is applied
/// </summary>
/// <remarks>
/// The mask is stored as <see cref="SizeX"/> rows of <see cref="SizeZ"/> bytes.
/// </remarks>
public sealed class TerrainPaintLayer
{
    const int maximumSize = byte.MaxValue * 4 + 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerrainPaintLayer"/> class with every mask value 0
    /// </summary>
    /// <param name="sizeX">The number of mask values along the lot's local X axis, which must be the section's <see cref="TerrainPaintSection.MaskSizeX"/></param>
    /// <param name="sizeZ">The number of mask values along the lot's local Z axis, which must be the section's <see cref="TerrainPaintSection.MaskSizeZ"/></param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sizeX"/> or <paramref name="sizeZ"/> is not 0 through 1,021, the mask size of a lot of 255 tiles</exception>
    public TerrainPaintLayer(int sizeX, int sizeZ)
    {
        if (sizeX is < 0 or > maximumSize)
            throw new ArgumentOutOfRangeException(nameof(sizeX), sizeX, $"The size must be 0 through {maximumSize}");
        if (sizeZ is < 0 or > maximumSize)
            throw new ArgumentOutOfRangeException(nameof(sizeZ), sizeZ, $"The size must be 0 through {maximumSize}");
        SetMask(sizeX, sizeZ, new byte[sizeX * sizeZ]);
    }

    internal TerrainPaintLayer()
    {
    }

    /// <summary>
    /// Gets/sets the mask value at a point
    /// </summary>
    /// <param name="x">The index along the lot's local X axis</param>
    /// <param name="z">The index along the lot's local Z axis</param>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate is outside the mask</exception>
    public byte this[int x, int z]
    {
        get => Mask[GetIndex(x, z)];
        set => Mask[GetIndex(x, z)] = value;
    }

    /// <summary>
    /// Gets/sets whether the layer has a mask, written from version 3
    /// </summary>
    /// <remarks>
    /// The game's writer writes <see langword="true"/> and the mask, or <see langword="false"/> and no mask, but its reader reads a mask either way, and so does this library.
    /// It is <see langword="true"/> in every lot examined.
    /// </remarks>
    public bool HasMask { get; set; } = true;

    /// <summary>
    /// Gets/sets the key of the paint, a CTProductTerrainPaint (0xEBCBB16C)
    /// </summary>
    public ResourceKey Key { get; set; }

    internal byte[] Mask { get; private set; } = [];

    /// <summary>
    /// Gets the number of mask values along the lot's local X axis
    /// </summary>
    public int SizeX { get; private set; }

    /// <summary>
    /// Gets the number of mask values along the lot's local Z axis
    /// </summary>
    public int SizeZ { get; private set; }

    /// <summary>
    /// Gets/sets the number of uses of the layer
    /// </summary>
    /// <remarks>
    /// The game's writer drops the layers whose count is 0.
    /// It equals the number of nonzero mask values in every layer examined; this library does not recompute it.
    /// </remarks>
    public uint UseCount { get; set; }

    int GetIndex(int x, int z)
    {
        if ((uint)x >= (uint)SizeX)
            throw new ArgumentOutOfRangeException(nameof(x), x, $"The X index must be less than {SizeX}");
        if ((uint)z >= (uint)SizeZ)
            throw new ArgumentOutOfRangeException(nameof(z), z, $"The Z index must be less than {SizeZ}");
        return x * SizeZ + z;
    }

    internal void SetMask(int sizeX, int sizeZ, byte[] mask)
    {
        SizeX = sizeX;
        SizeZ = sizeZ;
        Mask = mask;
    }
}
