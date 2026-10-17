namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class FloorIdGridSectionTests
{
    static byte[] Grid(int sizeX, int sizeZ, int levelCount)
    {
        var data = new byte[sizeX * sizeZ * levelCount * 8];
        for (var i = 0; i < data.Length / 2; ++i)
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(i * 2), (ushort)(i + 1));
        return data;
    }

    [TestMethod]
    public void ConstructorRejectsLayersForSingleLayerTypes() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new FloorIdGridSection(LotArchitectureSectionType.TerrainFloors, 2, 2, 2));

    [TestMethod]
    public void ConstructorRejectsOtherTypes() =>
        Assert.ThrowsExactly<ArgumentException>(() => new FloorIdGridSection(LotArchitectureSectionType.FloorRotations, 1, 1, 1));

    [TestMethod]
    [DataRow(LotArchitectureSectionType.Floors, 2)]
    [DataRow(LotArchitectureSectionType.Ceilings, 2)]
    [DataRow(LotArchitectureSectionType.PoolSurfaces, 2)]
    [DataRow(LotArchitectureSectionType.SecondaryFloors, 2)]
    [DataRow(LotArchitectureSectionType.CeilingPaints, 2)]
    [DataRow(LotArchitectureSectionType.TerrainFloors, 1)]
    [DataRow(LotArchitectureSectionType.PondBottoms, 1)]
    public void DecodeAndEncodeRoundTrip(LotArchitectureSectionType type, int levelCount)
    {
        var data = Grid(2, 3, levelCount);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(2, 3, 2)), (type, data));
        var grid = (FloorIdGridSection)architecture.Sections.Single(section => section.Type == type);
        Assert.AreEqual(2, grid.SizeX);
        Assert.AreEqual(3, grid.SizeZ);
        Assert.AreEqual(levelCount, grid.LevelCount);
        Assert.AreEqual(levelCount == 2, grid.HasLevels);
        Assert.AreEqual((ushort)1, grid[0, 0, 0, FloorQuarter.NegativeZ]);
        Assert.AreEqual((ushort)4, grid[0, 0, 0, FloorQuarter.NegativeX]);
        Assert.AreEqual((ushort)(((0 * 2 + 1) * 3 + 2) * 4 + 3), grid[1, 2, 0, FloorQuarter.PositiveZ]);
        Assert.AreEqual((ushort)(((levelCount - 1) * 2 * 3 + 1 * 3 + 0) * 4 + 2), grid[1, 0, levelCount - 1, FloorQuarter.PositiveX]);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, type));
    }

    [TestMethod]
    public void DecodeLeavesRecordWithoutDimensionsUndecoded()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotSize, BitConverter.GetBytes(2U)), (LotArchitectureSectionType.Floors, Grid(2, 3, 2)));
        Assert.IsNull(architecture.Floors);
        Assert.AreEqual(1, architecture.UndecodedRecords.Count);
    }

    [TestMethod]
    public void DecodeRejectsShortData() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(2, 3, 2)), (LotArchitectureSectionType.Floors, Grid(2, 3, 1))));

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(2, 3, 2)), (LotArchitectureSectionType.Floors, [.. Grid(2, 3, 2), 0])));

    [TestMethod]
    public void EncodeRejectsMismatchedDimensions()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(2, 3, 2)));
        architecture.Floors = new FloorIdGridSection(LotArchitectureSectionType.Floors, 2, 3, 1);
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    [DataRow(2, 0, 0, FloorQuarter.NegativeZ)]
    [DataRow(0, 3, 0, FloorQuarter.NegativeZ)]
    [DataRow(0, 0, 1, FloorQuarter.NegativeZ)]
    [DataRow(-1, 0, 0, FloorQuarter.NegativeZ)]
    [DataRow(0, 0, 0, (FloorQuarter)4)]
    public void IndexerRejectsOutOfRangeCoordinates(int x, int z, int levelIndex, FloorQuarter quarter) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new FloorIdGridSection(LotArchitectureSectionType.Floors, 2, 3, 1)[x, z, levelIndex, quarter]);

    [TestMethod]
    public void IndexerSetsOneQuarter()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(2, 3, 1)));
        var grid = new FloorIdGridSection(LotArchitectureSectionType.Floors, 2, 3, 1);
        grid[1, 2, 0, FloorQuarter.PositiveX] = 0x1234;
        architecture.Floors = grid;
        var expected = new byte[48];
        expected[((1 * 3) + 2) * 8 + 2] = 0x34;
        expected[((1 * 3) + 2) * 8 + 3] = 0x12;
        CollectionAssert.AreEqual(expected, TestBundles.Encode(architecture, LotArchitectureSectionType.Floors));
    }

    [TestMethod]
    [DataRow(8U, 1)]
    [DataRow(9U, 3)]
    public void PoolSurfacesLayerCountFollowsLotSizeVersion(uint lotSizeVersion, int levelCount)
    {
        var data = Grid(2, 2, levelCount);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(2, 2, 3, lotSizeVersion)), (LotArchitectureSectionType.PoolSurfaces, data));
        Assert.AreEqual(levelCount, architecture.PoolSurfaces!.LevelCount);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.PoolSurfaces));
    }
}
