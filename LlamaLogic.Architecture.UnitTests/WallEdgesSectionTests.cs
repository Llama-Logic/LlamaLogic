namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class WallEdgesSectionTests
{
    static LotArchitecture Decode(uint graphsVersion, byte[] data) =>
        TestBundles.Decode((LotArchitectureSectionType.VertexGraphs, VertexGraphsSectionTests.Section(graphsVersion)), (LotArchitectureSectionType.WallEdges, data));

    static byte[] Section(uint graphsVersion)
    {
        var bytes = new TestBytes().U32(2).U32(0x03020100).U16(1).U16(0).U32(0x03020600).U16(1).U16(1);
        Table(bytes, 0xD5F0F921, 0x10);
        if (graphsVersion > 1)
        {
            Table(bytes, 0x0418FE2A, 0x20);
            bytes.U32(1).U32(0x03020102).U32(1);
        }
        if (graphsVersion == 6)
            bytes.U32(2).U32(7).U8(1).U32(8).U8(0);
        if (graphsVersion > 8)
        {
            Table(bytes, 0x9151E6BC, 0x30);
            bytes.U32(1).U32(0x03020100).U32(0);
        }
        return bytes.ToArray();
    }

    static TestBytes Table(TestBytes bytes, uint type, ulong instance) =>
        bytes.KeyInstanceTypeGroup(0, 0, 0).U32(1).KeyInstanceTypeGroup(type, 0, instance).U32(1).U32(2);

    [TestMethod]
    [DataRow(5U)]
    [DataRow(6U)]
    [DataRow(7U)]
    [DataRow(8U)]
    [DataRow(9U)]
    public void DecodeAndEncodeRoundTrip(uint graphsVersion)
    {
        var data = Section(graphsVersion);
        var architecture = Decode(graphsVersion, data);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.WallEdges));
    }

    [TestMethod]
    public void DecodeReadsFields()
    {
        var section = Decode(9, Section(9)).WallEdges!;
        Assert.AreEqual(2, section.WallPatternEdges.Count);
        Assert.AreEqual(0x03020600U, section.WallPatternEdges[1].MapKey);
        Assert.AreEqual((ushort)1, section.WallPatternEdges[1].WallPattern1Id);
        Assert.AreEqual((ushort)1, section.WallPatternEdges[1].WallPattern2Id);
        Assert.AreEqual(new ResourceKey((ResourceType)0xD5F0F921, 0, 0x10), section.WallPatterns.Entries.Single().Key);
        Assert.AreEqual(new ResourceKey((ResourceType)0x0418FE2A, 0, 0x20), section.Products.Entries.Single().Key);
        Assert.AreEqual(0x03020102U, section.ProductEdges.Single().MapKey);
        Assert.AreEqual(1U, section.ProductEdges.Single().ProductId);
        Assert.AreEqual(0, section.LegacyEntries.Count);
        Assert.AreEqual(new ResourceKey((ResourceType)0x9151E6BC, 0, 0x30), section.AdditionalProducts.Entries.Single().Key);
        Assert.AreEqual(0U, section.AdditionalProductEdges.Single().ProductId);
    }

    [TestMethod]
    public void DecodeReadsLegacyEntries()
    {
        var entries = Decode(6, Section(6)).WallEdges!.LegacyEntries;
        Assert.AreEqual(2, entries.Count);
        Assert.AreEqual(7U, entries[0].Unknown1);
        Assert.AreEqual((byte)1, entries[0].Unknown2);
    }

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => Decode(8, Section(9)));

    [TestMethod]
    public void DecodeRejectsTruncatedSection() =>
        Assert.ThrowsExactly<InvalidDataException>(() => Decode(9, Section(8)));

    [TestMethod]
    public void DecodeWithoutVertexGraphsKeepsRecordUndecoded()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.WallEdges, Section(9)));
        Assert.IsNull(architecture.WallEdges);
        Assert.AreEqual((ResourceType)(uint)LotArchitectureSectionType.WallEdges, architecture.UndecodedRecords.Single().Key.Type);
    }

    [TestMethod]
    public void EncodeFollowsVertexGraphsVersion()
    {
        var architecture = Decode(9, Section(9));
        architecture.VertexGraphs = TestBundles.Decode((LotArchitectureSectionType.VertexGraphs, VertexGraphsSectionTests.Section(8))).VertexGraphs;
        architecture.WallEdges!.AdditionalProducts.Entries.Clear();
        architecture.WallEdges.AdditionalProductEdges.Clear();
        CollectionAssert.AreEqual(Section(8), TestBundles.Encode(architecture, LotArchitectureSectionType.WallEdges));
    }

    [TestMethod]
    [DataRow(9U, 8U)]
    [DataRow(6U, 7U)]
    public void EncodeRejectsEntriesTheVertexGraphsVersionCannotStore(uint decodedVersion, uint encodedVersion)
    {
        var architecture = Decode(decodedVersion, Section(decodedVersion));
        architecture.VertexGraphs = TestBundles.Decode((LotArchitectureSectionType.VertexGraphs, VertexGraphsSectionTests.Section(encodedVersion))).VertexGraphs;
        Assert.ThrowsExactly<InvalidOperationException>(() => TestBundles.Encode(architecture, LotArchitectureSectionType.WallEdges));
    }

    [TestMethod]
    public void EncodeRequiresVertexGraphs()
    {
        var architecture = Decode(9, Section(9));
        architecture.VertexGraphs = null;
        Assert.ThrowsExactly<InvalidOperationException>(() => TestBundles.Encode(architecture, LotArchitectureSectionType.WallEdges));
    }
}
