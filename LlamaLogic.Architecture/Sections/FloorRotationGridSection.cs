namespace LlamaLogic.Architecture;

/// <summary>
/// Represents one of the five floor rotation grids: the rotation of the pattern on each quarter of each tile of a <see cref="FloorIdGridSection"/> (🔓)
/// </summary>
/// <remarks>
/// <para>The grid has <see cref="SizeX"/> × <see cref="SizeZ"/> tiles per layer and one rotation of 0 through 3 per <see cref="FloorQuarter"/>.
/// A rotation is the number of 90° clockwise turns of the pattern, as seen in game: rotating one floor tile by 90° changed all four of its quarters from 0 to 1, and so did turning a terrain floor tile 90° clockwise (in-game tests).
/// Each grid is paired with a floor id grid: <see cref="LotArchitectureSectionType.FloorRotations"/> with <see cref="LotArchitectureSectionType.Floors"/>, <see cref="LotArchitectureSectionType.CeilingRotations"/> with <see cref="LotArchitectureSectionType.Ceilings"/>, <see cref="LotArchitectureSectionType.SecondaryFloorRotations"/> with <see cref="LotArchitectureSectionType.SecondaryFloors"/>, <see cref="LotArchitectureSectionType.CeilingPaintRotations"/> with <see cref="LotArchitectureSectionType.CeilingPaints"/> and <see cref="LotArchitectureSectionType.TerrainFloorRotations"/> with <see cref="LotArchitectureSectionType.TerrainFloors"/>; <see cref="FloorIdGridSection"/> describes the roles of the id grids and the in-game tests and code that show them.</para>
/// <para>All types but <see cref="LotArchitectureSectionType.TerrainFloorRotations"/> have one layer per level of the lot (<see cref="HasLevels"/>), indexed by level − <see cref="LotInfoSection.LowestLevel"/>; <see cref="LotArchitectureSectionType.TerrainFloorRotations"/> has a single layer, for ground level.</para>
/// <para>The layers are stored one after another, each as <see cref="SizeX"/> rows of <see cref="SizeZ"/> bytes; each byte holds the four rotations of a tile, two bits each, the <see cref="FloorQuarter.NegativeZ"/> quarter in the lowest bits, as the game's getter and setter read and write them. The section has no version.</para>
/// </remarks>
public sealed class FloorRotationGridSection :
    LotArchitectureSection
{
    internal static FloorRotationGridSection Read(ref SectionReader reader, SectionContext context)
    {
        var type = reader.Type;
        var sizeX = context.RequireSizeX(type);
        var sizeZ = context.RequireSizeZ(type);
        var levelCount = GetRequiredLevelCount(type, context);
        return new FloorRotationGridSection(type, sizeX, sizeZ, levelCount, reader.ReadBytes(sizeX * sizeZ * levelCount));
    }

    static int GetRequiredLevelCount(LotArchitectureSectionType type, SectionContext context) =>
        IsSingleLayer(type) ? 1 : context.RequireLevelCount(type);

    static bool IsSingleLayer(LotArchitectureSectionType type) =>
        type is LotArchitectureSectionType.TerrainFloorRotations;

    static LotArchitectureSectionType RequireType(LotArchitectureSectionType type) =>
        type is LotArchitectureSectionType.FloorRotations
            or LotArchitectureSectionType.CeilingRotations
            or LotArchitectureSectionType.SecondaryFloorRotations
            or LotArchitectureSectionType.CeilingPaintRotations
            or LotArchitectureSectionType.TerrainFloorRotations
            ? type
            : throw new ArgumentException($"{type} is not a floor rotation grid type", nameof(type));

    /// <summary>
    /// Initializes a new instance of the <see cref="FloorRotationGridSection"/> class with every rotation 0
    /// </summary>
    /// <param name="type">The type of the section, which must be one of the floor rotation grid types</param>
    /// <param name="sizeX">The number of tiles along the lot's local X axis</param>
    /// <param name="sizeZ">The number of tiles along the lot's local Z axis</param>
    /// <param name="levelCount">The number of layers: the lot's level count, or 1 for a grid with a single layer</param>
    /// <exception cref="ArgumentException"><paramref name="type"/> is not a floor rotation grid type</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sizeX"/>, <paramref name="sizeZ"/> or <paramref name="levelCount"/> is not 0 through 255, or <paramref name="levelCount"/> is not 1 for a type with no levels</exception>
    public FloorRotationGridSection(LotArchitectureSectionType type, int sizeX, int sizeZ, int levelCount) :
        this(type, sizeX, sizeZ, levelCount, null)
    {
    }

    FloorRotationGridSection(LotArchitectureSectionType type, int sizeX, int sizeZ, int levelCount, byte[]? data) :
        base(RequireType(type))
    {
        if (sizeX is < 0 or > byte.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(sizeX), sizeX, "The size must be 0 through 255");
        if (sizeZ is < 0 or > byte.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(sizeZ), sizeZ, "The size must be 0 through 255");
        if (levelCount is < 0 or > byte.MaxValue || IsSingleLayer(type) && levelCount != 1)
            throw new ArgumentOutOfRangeException(nameof(levelCount), levelCount, IsSingleLayer(type) ? $"A {type} grid has a single layer" : "The level count must be 0 through 255");
        SizeX = sizeX;
        SizeZ = sizeZ;
        LevelCount = levelCount;
        this.data = data ?? new byte[sizeX * sizeZ * levelCount];
    }

    readonly byte[] data;

    /// <summary>
    /// Gets/sets the rotation of the pattern on a quarter of a tile, as the number of 90° clockwise turns
    /// </summary>
    /// <param name="x">The tile's index along the lot's local X axis</param>
    /// <param name="z">The tile's index along the lot's local Z axis</param>
    /// <param name="levelIndex">The layer: the level − <see cref="LotInfoSection.LowestLevel"/>, or 0 for a grid with a single layer</param>
    /// <param name="quarter">The quarter of the tile</param>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate is outside the grid, <paramref name="quarter"/> is not a <see cref="FloorQuarter"/>, or the rotation being set is not 0 through 3</exception>
    public byte this[int x, int z, int levelIndex, FloorQuarter quarter]
    {
        get => (byte)((data[GetOffset(x, z, levelIndex, quarter)] >> ((int)quarter * 2)) & 3);
        set
        {
            if (value > 3)
                throw new ArgumentOutOfRangeException(nameof(value), value, "The rotation must be 0 through 3");
            var offset = GetOffset(x, z, levelIndex, quarter);
            var shift = (int)quarter * 2;
            data[offset] = (byte)((data[offset] & ~(3 << shift)) | (value << shift));
        }
    }

    /// <summary>
    /// Gets whether the grid has one layer per level of the lot, which all types but <see cref="LotArchitectureSectionType.TerrainFloorRotations"/> do
    /// </summary>
    public bool HasLevels =>
        !IsSingleLayer(Type);

    /// <summary>
    /// Gets the number of layers of the grid
    /// </summary>
    public int LevelCount { get; }

    /// <summary>
    /// Gets the number of tiles of the grid along the lot's local X axis
    /// </summary>
    public int SizeX { get; }

    /// <summary>
    /// Gets the number of tiles of the grid along the lot's local Z axis
    /// </summary>
    public int SizeZ { get; }

    int GetOffset(int x, int z, int levelIndex, FloorQuarter quarter)
    {
        if ((uint)x >= (uint)SizeX)
            throw new ArgumentOutOfRangeException(nameof(x), x, $"The X index must be less than {SizeX}");
        if ((uint)z >= (uint)SizeZ)
            throw new ArgumentOutOfRangeException(nameof(z), z, $"The Z index must be less than {SizeZ}");
        if ((uint)levelIndex >= (uint)LevelCount)
            throw new ArgumentOutOfRangeException(nameof(levelIndex), levelIndex, $"The level index must be less than {LevelCount}");
        if (quarter > FloorQuarter.NegativeX)
            throw new ArgumentOutOfRangeException(nameof(quarter), quarter, "The quarter is not valid");
        return ((levelIndex * SizeX) + x) * SizeZ + z;
    }

    internal override void Write(SectionWriter writer, SectionContext context)
    {
        var sizeX = context.RequireSizeX(Type);
        var sizeZ = context.RequireSizeZ(Type);
        var levelCount = GetRequiredLevelCount(Type, context);
        if (SizeX != sizeX || SizeZ != sizeZ || LevelCount != levelCount)
            throw writer.Invalid($"the grid is {SizeX} × {SizeZ} × {LevelCount}, but the lot needs {sizeX} × {sizeZ} × {levelCount}");
        writer.Write(data);
    }
}
