namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class TerrainHeightsSectionTests
{
    static byte[] Record(int sizeX, int sizeZ, int vertexGridCount, bool hasWaterLevels)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        for (var i = 0; i < vertexGridCount * (sizeX + 1) * (sizeZ + 1); ++i)
            writer.Write(i * 0.25f);
        if (hasWaterLevels)
            for (var i = 0; i < sizeX * sizeZ * 4; ++i)
                writer.Write(-400 + i);
        writer.Flush();
        return stream.ToArray();
    }

    [TestMethod]
    public void ConstructorWithDimensionsUsesTheCurrentLayout()
    {
        var section = new TerrainHeightsSection(2, 3);
        Assert.AreEqual(2, section.VertexGridCount);
        Assert.IsTrue(section.HasWaterLevels);
        Assert.AreEqual(-400, section.GetWaterLevel(1, 2, FloorQuarter.NegativeX));
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(2, 3, 1)));
        architecture.TerrainHeights = section;
        Assert.AreEqual(2 * 3 * 4 * 4 + 3 * 4 * 2 * 4, TestBundles.Encode(architecture, LotArchitectureSectionType.TerrainHeights).Length);
    }

    [TestMethod]
    [DataRow(9U, 3, false)]
    [DataRow(10U, 2, false)]
    [DataRow(12U, 2, true)]
    [DataRow(13U, 2, true)]
    public void DecodeAndEncodeRoundTrip(uint lotSizeVersion, int vertexGridCount, bool hasWaterLevels)
    {
        var data = Record(2, 3, vertexGridCount, hasWaterLevels);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(2, 3, 3, lotSizeVersion)), (LotArchitectureSectionType.TerrainHeights, data));
        var section = architecture.TerrainHeights!;
        Assert.AreEqual(2, section.SizeX);
        Assert.AreEqual(3, section.SizeZ);
        Assert.AreEqual(vertexGridCount, section.VertexGridCount);
        Assert.AreEqual(hasWaterLevels, section.HasWaterLevels);
        Assert.AreEqual(((1 * 3 + 2) * 4 + 3) * 0.25f, section.GetVertexValue(1, 2, 3));
        Assert.AreEqual(((0 * 3 + 2) * 4 + 1) * 0.25f, section.GetVertexValue(0, 2, 1));
        if (hasWaterLevels)
            Assert.AreEqual(-400 + (1 * 3 + 2) * 4 + 2, section.GetWaterLevel(1, 2, FloorQuarter.PositiveZ));
        else
            Assert.ThrowsExactly<InvalidOperationException>(() => section.GetWaterLevel(0, 0, FloorQuarter.NegativeZ));
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.TerrainHeights));
    }

    [TestMethod]
    public void DecodeRejectsShortData() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(2, 3, 1)), (LotArchitectureSectionType.TerrainHeights, Record(2, 3, 2, false))));

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(2, 3, 1)), (LotArchitectureSectionType.TerrainHeights, [.. Record(2, 3, 2, true), 0])));

    [TestMethod]
    [DataRow(2, 3, 2, false)]
    [DataRow(2, 3, 3, true)]
    [DataRow(3, 3, 2, true)]
    public void EncodeRejectsMismatchedLayout(int sizeX, int sizeZ, int vertexGridCount, bool hasWaterLevels)
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(2, 3, 3)));
        architecture.TerrainHeights = new TerrainHeightsSection(sizeX, sizeZ, vertexGridCount, hasWaterLevels);
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void NewSectionHasNoWater()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(1, 1, 1)));
        var section = new TerrainHeightsSection(1, 1, 2, true);
        section.SetVertexValue(1, 1, 0, 1.5f);
        section.SetWaterLevel(0, 0, FloorQuarter.NegativeX, -4);
        architecture.TerrainHeights = section;
        var expected = new byte[2 * 4 * 4 + 4 * 4];
        BinaryPrimitives.WriteSingleLittleEndian(expected.AsSpan((4 + 1 * 2 + 0) * 4), 1.5f);
        for (var i = 0; i < 3; ++i)
            BinaryPrimitives.WriteInt32LittleEndian(expected.AsSpan(32 + i * 4), -400);
        BinaryPrimitives.WriteInt32LittleEndian(expected.AsSpan(44), -4);
        CollectionAssert.AreEqual(expected, TestBundles.Encode(architecture, LotArchitectureSectionType.TerrainHeights));
    }

    [TestMethod]
    public void VertexAccessRejectsOutOfRangeCoordinates() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TerrainHeightsSection(2, 3, 2, true).GetVertexValue(0, 3, 0));
}
