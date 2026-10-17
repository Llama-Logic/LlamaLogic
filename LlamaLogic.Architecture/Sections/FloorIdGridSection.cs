namespace LlamaLogic.Architecture;

/// <summary>
/// Represents one of the seven floor id grids: the <see cref="FloorKeysSection"/> id painted on each quarter of each tile (🔓)
/// </summary>
/// <remarks>
/// <para>The grid has <see cref="SizeX"/> × <see cref="SizeZ"/> tiles per layer and four ids per tile, one per <see cref="FloorQuarter"/>.
/// Each id is a <see cref="FloorKeyEntry.Id"/> of the lot's <see cref="FloorKeysSection"/>, whose key is a CTProductFloorPattern (0xB4F762C9); 0 means no floor.</para>
/// <para>The types <see cref="LotArchitectureSectionType.Floors"/>, <see cref="LotArchitectureSectionType.Ceilings"/>, <see cref="LotArchitectureSectionType.PoolSurfaces"/>, <see cref="LotArchitectureSectionType.SecondaryFloors"/> and <see cref="LotArchitectureSectionType.CeilingPaints"/> have one layer per level of the lot (<see cref="HasLevels"/>), indexed by level − <see cref="LotInfoSection.LowestLevel"/>, so ground level is index −<see cref="LotInfoSection.LowestLevel"/>.
/// <see cref="LotArchitectureSectionType.TerrainFloors"/> and <see cref="LotArchitectureSectionType.PondBottoms"/> have a single layer, for ground level.
/// When the lot's <see cref="LotSizeSection.Version"/> is 8 or less, <see cref="LotArchitectureSectionType.PoolSurfaces"/> also has a single layer, for ground level.</para>
/// <para>Roles proven by the game's code and in-game tests: <see cref="LotArchitectureSectionType.Floors"/> is the floor pattern of each level, <see cref="LotArchitectureSectionType.TerrainFloors"/> the floor pattern painted on bare terrain, <see cref="LotArchitectureSectionType.PondBottoms"/> the pond bottom pattern and <see cref="LotArchitectureSectionType.CeilingPaints"/> the ceiling paint.
/// In-game tests show that building a room fills <see cref="LotArchitectureSectionType.Ceilings"/> at the level above the room over exactly the room's tiles (and painting the ceiling changes only <see cref="LotArchitectureSectionType.CeilingPaints"/>), and that building a pool fills <see cref="LotArchitectureSectionType.PoolSurfaces"/> over exactly the pool's tiles at the level above the pool's block; the game's code assigns both grids to the room on the level below their layer.
/// In-game tests show that building a room or a platform fills <see cref="LotArchitectureSectionType.SecondaryFloors"/> over its tiles with the lot's default floor key, and that painting a floor or a platform's top changes only <see cref="LotArchitectureSectionType.Floors"/>; the role of <see cref="LotArchitectureSectionType.SecondaryFloors"/>, which covers the same quarters as <see cref="LotArchitectureSectionType.Floors"/>, is still unknown.</para>
/// <para>The layers are stored one after another, each as <see cref="SizeX"/> rows of <see cref="SizeZ"/> tiles of four little-endian 16-bit ids; the section has no version.</para>
/// </remarks>
public sealed class FloorIdGridSection :
    LotArchitectureSection
{
    const int tileSize = 8;

    internal static FloorIdGridSection Read(ref SectionReader reader, SectionContext context)
    {
        var type = reader.Type;
        var sizeX = context.RequireSizeX(type);
        var sizeZ = context.RequireSizeZ(type);
        var levelCount = GetRequiredLevelCount(type, context);
        return new FloorIdGridSection(type, sizeX, sizeZ, levelCount, reader.ReadBytes(sizeX * sizeZ * levelCount * tileSize));
    }

    static int GetRequiredLevelCount(LotArchitectureSectionType type, SectionContext context) =>
        IsSingleLayer(type) || type is LotArchitectureSectionType.PoolSurfaces && context.RequireLotSizeVersion(type) <= 8 ? 1 : context.RequireLevelCount(type);

    static bool IsSingleLayer(LotArchitectureSectionType type) =>
        type is LotArchitectureSectionType.TerrainFloors or LotArchitectureSectionType.PondBottoms;

    static LotArchitectureSectionType RequireType(LotArchitectureSectionType type) =>
        type is LotArchitectureSectionType.Floors
            or LotArchitectureSectionType.Ceilings
            or LotArchitectureSectionType.PoolSurfaces
            or LotArchitectureSectionType.SecondaryFloors
            or LotArchitectureSectionType.CeilingPaints
            or LotArchitectureSectionType.TerrainFloors
            or LotArchitectureSectionType.PondBottoms
            ? type
            : throw new ArgumentException($"{type} is not a floor id grid type", nameof(type));

    /// <summary>
    /// Initializes a new instance of the <see cref="FloorIdGridSection"/> class with every id 0
    /// </summary>
    /// <param name="type">The type of the section, which must be one of the floor id grid types</param>
    /// <param name="sizeX">The number of tiles along the lot's local X axis</param>
    /// <param name="sizeZ">The number of tiles along the lot's local Z axis</param>
    /// <param name="levelCount">The number of layers: the lot's level count, or 1 for a grid with a single layer</param>
    /// <exception cref="ArgumentException"><paramref name="type"/> is not a floor id grid type</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sizeX"/>, <paramref name="sizeZ"/> or <paramref name="levelCount"/> is not 0 through 255, or <paramref name="levelCount"/> is not 1 for a type with no levels</exception>
    public FloorIdGridSection(LotArchitectureSectionType type, int sizeX, int sizeZ, int levelCount) :
        this(type, sizeX, sizeZ, levelCount, null)
    {
    }

    FloorIdGridSection(LotArchitectureSectionType type, int sizeX, int sizeZ, int levelCount, byte[]? data) :
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
        this.data = data ?? new byte[sizeX * sizeZ * levelCount * tileSize];
    }

    readonly byte[] data;

    /// <summary>
    /// Gets/sets the floor key id of a quarter of a tile
    /// </summary>
    /// <param name="x">The tile's index along the lot's local X axis</param>
    /// <param name="z">The tile's index along the lot's local Z axis</param>
    /// <param name="levelIndex">The layer: the level − <see cref="LotInfoSection.LowestLevel"/>, or 0 for a grid with a single layer</param>
    /// <param name="quarter">The quarter of the tile</param>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate is outside the grid, or <paramref name="quarter"/> is not a <see cref="FloorQuarter"/></exception>
    public ushort this[int x, int z, int levelIndex, FloorQuarter quarter]
    {
        get => BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(GetOffset(x, z, levelIndex, quarter)));
        set => BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(GetOffset(x, z, levelIndex, quarter)), value);
    }

    /// <summary>
    /// Gets whether the grid has one layer per level of the lot, which all types but <see cref="LotArchitectureSectionType.TerrainFloors"/> and <see cref="LotArchitectureSectionType.PondBottoms"/> do
    /// </summary>
    /// <remarks>
    /// A <see cref="LotArchitectureSectionType.PoolSurfaces"/> grid of a lot whose <see cref="LotSizeSection.Version"/> is 8 or less has levels but only one layer, for ground level.
    /// </remarks>
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
        return (((levelIndex * SizeX) + x) * SizeZ + z) * tileSize + (int)quarter * 2;
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
