namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.TerrainHeights"/> section: the lot's terrain height grid, a second vertex grid of unknown role and a water level per tile quarter (🔓)
/// </summary>
/// <remarks>
/// <para>The layout depends on the lot's <see cref="LotSizeSection.Version"/>; the section has no version of its own.
/// First come <see cref="VertexGridCount"/> grids of (<see cref="SizeX"/> + 1) × (<see cref="SizeZ"/> + 1) 32-bit floats, one per tile corner, stored as <see cref="SizeX"/> + 1 rows of <see cref="SizeZ"/> + 1 values.
/// Then, when <see cref="HasWaterLevels"/>, four 32-bit integers per tile, one per <see cref="FloorQuarter"/>, the tiles stored as <see cref="SizeX"/> rows of <see cref="SizeZ"/>.</para>
/// <para>When the lot's <see cref="LotSizeSection.Version"/> is less than 10 there is one vertex grid per level, indexed by level − <see cref="LotInfoSection.LowestLevel"/>, and the game skips all but the ground level's grid, which holds the terrain height.
/// From version 10 there are two: grid 0 is the terrain height (the game's terrain height getter and setter use it, and in-game tests show that sculpting the terrain changes only this grid) and grid 1 is a second per-corner grid whose role is unknown; in-game tests with the raise, flatten and smooth tools changed only grid 0 and left grid 1 unchanged.
/// The water levels are present from version 12; before it the game fills them with −400, which means no water.</para>
/// </remarks>
public sealed class TerrainHeightsSection :
    LotArchitectureSection
{
    const int noWaterLevel = -400;

    internal static TerrainHeightsSection Read(ref SectionReader reader, SectionContext context)
    {
        var type = reader.Type;
        var sizeX = context.RequireSizeX(type);
        var sizeZ = context.RequireSizeZ(type);
        var (vertexGridCount, hasWaterLevels) = GetRequiredLayout(type, context);
        return new TerrainHeightsSection(sizeX, sizeZ, vertexGridCount, hasWaterLevels, reader.ReadBytes(GetDataLength(sizeX, sizeZ, vertexGridCount, hasWaterLevels)));
    }

    static int GetDataLength(int sizeX, int sizeZ, int vertexGridCount, bool hasWaterLevels) =>
        GetVertexDataLength(sizeX, sizeZ, vertexGridCount) + (hasWaterLevels ? sizeX * sizeZ * 16 : 0);

    static (int vertexGridCount, bool hasWaterLevels) GetRequiredLayout(LotArchitectureSectionType type, SectionContext context)
    {
        var lotSizeVersion = context.RequireLotSizeVersion(type);
        return (lotSizeVersion < 10 ? context.RequireLevelCount(type) : 2, lotSizeVersion >= 12);
    }

    static int GetVertexDataLength(int sizeX, int sizeZ, int vertexGridCount) =>
        (sizeX + 1) * (sizeZ + 1) * vertexGridCount * 4;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerrainHeightsSection"/> class in the layout of a lot whose <see cref="LotSizeSection.Version"/> is 12 or more, with two vertex grids and water levels, every vertex value 0 and every water level −400 (no water)
    /// </summary>
    /// <param name="sizeX">The number of tiles along the lot's local X axis</param>
    /// <param name="sizeZ">The number of tiles along the lot's local Z axis</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sizeX"/> or <paramref name="sizeZ"/> is not 0 through 255</exception>
    public TerrainHeightsSection(int sizeX, int sizeZ) :
        this(sizeX, sizeZ, 2, true)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TerrainHeightsSection"/> class with every vertex value 0 and every water level −400 (no water)
    /// </summary>
    /// <param name="sizeX">The number of tiles along the lot's local X axis</param>
    /// <param name="sizeZ">The number of tiles along the lot's local Z axis</param>
    /// <param name="vertexGridCount">The number of vertex grids: the lot's level count if the lot's <see cref="LotSizeSection.Version"/> is less than 10, otherwise 2</param>
    /// <param name="hasWaterLevels">Whether there are water levels, which there are if the lot's <see cref="LotSizeSection.Version"/> is 12 or more</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sizeX"/>, <paramref name="sizeZ"/> or <paramref name="vertexGridCount"/> is not 0 through 255</exception>
    public TerrainHeightsSection(int sizeX, int sizeZ, int vertexGridCount, bool hasWaterLevels) :
        this(sizeX, sizeZ, vertexGridCount, hasWaterLevels, null)
    {
    }

    TerrainHeightsSection(int sizeX, int sizeZ, int vertexGridCount, bool hasWaterLevels, byte[]? data) :
        base(LotArchitectureSectionType.TerrainHeights)
    {
        if (sizeX is < 0 or > byte.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(sizeX), sizeX, "The size must be 0 through 255");
        if (sizeZ is < 0 or > byte.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(sizeZ), sizeZ, "The size must be 0 through 255");
        if (vertexGridCount is < 0 or > byte.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(vertexGridCount), vertexGridCount, "The vertex grid count must be 0 through 255");
        SizeX = sizeX;
        SizeZ = sizeZ;
        VertexGridCount = vertexGridCount;
        HasWaterLevels = hasWaterLevels;
        if (data is null)
        {
            data = new byte[GetDataLength(sizeX, sizeZ, vertexGridCount, hasWaterLevels)];
            if (hasWaterLevels)
                for (var offset = GetVertexDataLength(sizeX, sizeZ, vertexGridCount); offset < data.Length; offset += 4)
                    BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(offset), noWaterLevel);
        }
        this.data = data;
    }

    readonly byte[] data;

    /// <summary>
    /// Gets whether the section holds a water level per tile quarter, which it does when the lot's <see cref="LotSizeSection.Version"/> is 12 or more
    /// </summary>
    public bool HasWaterLevels { get; }

    /// <summary>
    /// Gets the number of tiles along the lot's local X axis
    /// </summary>
    public int SizeX { get; }

    /// <summary>
    /// Gets the number of tiles along the lot's local Z axis
    /// </summary>
    public int SizeZ { get; }

    /// <summary>
    /// Gets the number of vertex grids
    /// </summary>
    public int VertexGridCount { get; }

    int GetVertexOffset(int gridIndex, int x, int z)
    {
        if ((uint)gridIndex >= (uint)VertexGridCount)
            throw new ArgumentOutOfRangeException(nameof(gridIndex), gridIndex, $"The grid index must be less than {VertexGridCount}");
        if ((uint)x > (uint)SizeX)
            throw new ArgumentOutOfRangeException(nameof(x), x, $"The X index must be {SizeX} or less");
        if ((uint)z > (uint)SizeZ)
            throw new ArgumentOutOfRangeException(nameof(z), z, $"The Z index must be {SizeZ} or less");
        return ((gridIndex * (SizeX + 1) + x) * (SizeZ + 1) + z) * 4;
    }

    /// <summary>
    /// Gets the value of a tile corner in a vertex grid
    /// </summary>
    /// <param name="gridIndex">The vertex grid: 0 for the terrain height when the lot's <see cref="LotSizeSection.Version"/> is 10 or more, otherwise the level − <see cref="LotInfoSection.LowestLevel"/></param>
    /// <param name="x">The corner's index along the lot's local X axis, 0 through <see cref="SizeX"/></param>
    /// <param name="z">The corner's index along the lot's local Z axis, 0 through <see cref="SizeZ"/></param>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate is outside the grid</exception>
    public float GetVertexValue(int gridIndex, int x, int z) =>
        BinaryPrimitives.ReadSingleLittleEndian(data.AsSpan(GetVertexOffset(gridIndex, x, z)));

    /// <summary>
    /// Gets the water level of a quarter of a tile, in steps of 0.25 m
    /// </summary>
    /// <param name="x">The tile's index along the lot's local X axis</param>
    /// <param name="z">The tile's index along the lot's local Z axis</param>
    /// <param name="quarter">The quarter of the tile</param>
    /// <remarks>
    /// −400 (the game's default) means no water, and the value is −400 almost everywhere.
    /// The game multiplies it by 0.25 and uses the result minus the floor or terrain height as a water depth.
    /// In the lots examined it differs from −400 on every quarter of every pond and then equals the pond's <see cref="Pond.WaterLevel"/>; raising a pond's water level one step in game changed it from −3 to −2 on every quarter of the pond together with <see cref="Pond.WaterLevel"/> (in-game test).
    /// </remarks>
    /// <exception cref="InvalidOperationException"><see cref="HasWaterLevels"/> is <see langword="false"/></exception>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate is outside the grid, or <paramref name="quarter"/> is not a <see cref="FloorQuarter"/></exception>
    public int GetWaterLevel(int x, int z, FloorQuarter quarter) =>
        BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(GetWaterLevelOffset(x, z, quarter)));

    int GetWaterLevelOffset(int x, int z, FloorQuarter quarter)
    {
        if (!HasWaterLevels)
            throw new InvalidOperationException("The section holds no water levels");
        if ((uint)x >= (uint)SizeX)
            throw new ArgumentOutOfRangeException(nameof(x), x, $"The X index must be less than {SizeX}");
        if ((uint)z >= (uint)SizeZ)
            throw new ArgumentOutOfRangeException(nameof(z), z, $"The Z index must be less than {SizeZ}");
        if (quarter > FloorQuarter.NegativeX)
            throw new ArgumentOutOfRangeException(nameof(quarter), quarter, "The quarter is not valid");
        return GetVertexDataLength(SizeX, SizeZ, VertexGridCount) + ((x * SizeZ + z) * 4 + (int)quarter) * 4;
    }

    /// <summary>
    /// Sets the value of a tile corner in a vertex grid
    /// </summary>
    /// <param name="gridIndex">The vertex grid (see <see cref="GetVertexValue(int, int, int)"/>)</param>
    /// <param name="x">The corner's index along the lot's local X axis, 0 through <see cref="SizeX"/></param>
    /// <param name="z">The corner's index along the lot's local Z axis, 0 through <see cref="SizeZ"/></param>
    /// <param name="value">The value</param>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate is outside the grid</exception>
    public void SetVertexValue(int gridIndex, int x, int z, float value) =>
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(GetVertexOffset(gridIndex, x, z)), value);

    /// <summary>
    /// Sets the water level of a quarter of a tile, in steps of 0.25 m
    /// </summary>
    /// <param name="x">The tile's index along the lot's local X axis</param>
    /// <param name="z">The tile's index along the lot's local Z axis</param>
    /// <param name="quarter">The quarter of the tile</param>
    /// <param name="value">The water level, or −400 for no water (see <see cref="GetWaterLevel(int, int, FloorQuarter)"/>)</param>
    /// <exception cref="InvalidOperationException"><see cref="HasWaterLevels"/> is <see langword="false"/></exception>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate is outside the grid, or <paramref name="quarter"/> is not a <see cref="FloorQuarter"/></exception>
    public void SetWaterLevel(int x, int z, FloorQuarter quarter, int value) =>
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(GetWaterLevelOffset(x, z, quarter)), value);

    internal override void Write(SectionWriter writer, SectionContext context)
    {
        var sizeX = context.RequireSizeX(Type);
        var sizeZ = context.RequireSizeZ(Type);
        var (vertexGridCount, hasWaterLevels) = GetRequiredLayout(Type, context);
        if (SizeX != sizeX || SizeZ != sizeZ)
            throw writer.Invalid($"the grids are {SizeX} × {SizeZ} tiles, but the lot is {sizeX} × {sizeZ}");
        if (VertexGridCount != vertexGridCount)
            throw writer.Invalid($"there are {VertexGridCount} vertex grids, but the lot needs {vertexGridCount}");
        if (HasWaterLevels != hasWaterLevels)
            throw writer.Invalid(hasWaterLevels ? "the lot needs water levels" : "the lot cannot have water levels");
        writer.Write(data);
    }
}
