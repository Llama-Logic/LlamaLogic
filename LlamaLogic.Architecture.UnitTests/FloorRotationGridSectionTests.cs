namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class FloorRotationGridSectionTests
{
    static byte[] Grid(int sizeX, int sizeZ, int levelCount)
    {
        var data = new byte[sizeX * sizeZ * levelCount];
        for (var i = 0; i < data.Length; ++i)
            data[i] = (byte)(i * 37 + 0x1b);
        return data;
    }

    [TestMethod]
    public void ConstructorRejectsLayersForSingleLayerTypes() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new FloorRotationGridSection(LotArchitectureSectionType.TerrainFloorRotations, 2, 2, 2));

    [TestMethod]
    public void ConstructorRejectsOtherTypes() =>
        Assert.ThrowsExactly<ArgumentException>(() => new FloorRotationGridSection(LotArchitectureSectionType.Floors, 1, 1, 1));

    [TestMethod]
    [DataRow(LotArchitectureSectionType.FloorRotations, 2)]
    [DataRow(LotArchitectureSectionType.CeilingRotations, 2)]
    [DataRow(LotArchitectureSectionType.SecondaryFloorRotations, 2)]
    [DataRow(LotArchitectureSectionType.CeilingPaintRotations, 2)]
    [DataRow(LotArchitectureSectionType.TerrainFloorRotations, 1)]
    public void DecodeAndEncodeRoundTrip(LotArchitectureSectionType type, int levelCount)
    {
        var data = Grid(2, 3, levelCount);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(2, 3, 2)), (type, data));
        var grid = (FloorRotationGridSection)architecture.Sections.Single(section => section.Type == type);
        Assert.AreEqual(2, grid.SizeX);
        Assert.AreEqual(3, grid.SizeZ);
        Assert.AreEqual(levelCount, grid.LevelCount);
        Assert.AreEqual(levelCount == 2, grid.HasLevels);
        var cell = data[((levelCount - 1) * 2 + 1) * 3 + 2];
        Assert.AreEqual((byte)(cell & 3), grid[1, 2, levelCount - 1, FloorQuarter.NegativeZ]);
        Assert.AreEqual((byte)((cell >> 2) & 3), grid[1, 2, levelCount - 1, FloorQuarter.PositiveX]);
        Assert.AreEqual((byte)((cell >> 4) & 3), grid[1, 2, levelCount - 1, FloorQuarter.PositiveZ]);
        Assert.AreEqual((byte)(cell >> 6), grid[1, 2, levelCount - 1, FloorQuarter.NegativeX]);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, type));
    }

    [TestMethod]
    public void DecodeRejectsShortData() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(2, 3, 2)), (LotArchitectureSectionType.FloorRotations, Grid(2, 3, 1))));

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(2, 3, 2)), (LotArchitectureSectionType.FloorRotations, [.. Grid(2, 3, 2), 0])));

    [TestMethod]
    public void EncodeRejectsMismatchedDimensions()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(2, 3, 2)));
        architecture.TerrainFloorRotations = new FloorRotationGridSection(LotArchitectureSectionType.TerrainFloorRotations, 3, 2, 1);
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void IndexerRejectsRotationsAbove3() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new FloorRotationGridSection(LotArchitectureSectionType.FloorRotations, 1, 1, 1)[0, 0, 0, FloorQuarter.NegativeZ] = 4);

    [TestMethod]
    public void IndexerSetsOneQuarter()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(2, 3, 1)));
        var grid = new FloorRotationGridSection(LotArchitectureSectionType.FloorRotations, 2, 3, 1);
        grid[1, 0, 0, FloorQuarter.PositiveZ] = 3;
        grid[1, 0, 0, FloorQuarter.NegativeZ] = 1;
        grid[1, 0, 0, FloorQuarter.PositiveZ] = 2;
        architecture.FloorRotations = grid;
        CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 0x21, 0, 0 }, TestBundles.Encode(architecture, LotArchitectureSectionType.FloorRotations));
    }
}
