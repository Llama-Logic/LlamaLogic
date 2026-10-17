namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class LegacyWallGraphSectionTests
{
    static byte[] Section(uint version = 1) =>
        new TestBytes().U32(version)
            .KeyInstanceTypeGroup(0, 0, 0).U32(2).KeyInstanceTypeGroup(0x9151E6BC, 0, 0x10).U32(1).U32(1).KeyInstanceTypeGroup(0x3F0C529A, 0, 0x11).U32(2).U32(1)
            .KeyInstanceTypeGroup(0, 0, 0).U32(1).KeyInstanceTypeGroup(0xD5F0F921, 0, 0x20).U32(1).U32(2)
            .I32(1).U32(0x00204081).I32(1).U16(1).U16(2).U16(512).U16(512).U32(1).U32(1).U32(1)
            .I32(1).U32(0x00204081).U32(1).U32(0).U32(0xCCCCCCCC).U32(0xCCCCCCCC).U32(0xCCCCCCCC).U32(2)
            .I32(0)
            .ToArray();

    [TestMethod]
    public void DecodeAndEncodeRoundTrip()
    {
        var data = Section();
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LegacyWallGraph, data));
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.LegacyWallGraph));
    }

    [TestMethod]
    public void DecodeAndEncodeRoundTripEmpty()
    {
        var data = new TestBytes().U32(1).KeyInstanceTypeGroup(0, 0, 0).U32(0).KeyInstanceTypeGroup(0, 0, 0).U32(0).I32(0).I32(0).I32(0).ToArray();
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LegacyWallGraph, data));
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.LegacyWallGraph));
    }

    [TestMethod]
    [DataRow(0U)]
    [DataRow(2U)]
    public void DecodeKeepsUnsupportedVersionsUndecoded(uint version) =>
        TestBundles.AssertUndecoded(LotArchitectureSectionType.LegacyWallGraph, Section(version), architecture => architecture.LegacyWallGraph);

    [TestMethod]
    public void DecodeReadsFields()
    {
        var section = TestBundles.Decode((LotArchitectureSectionType.LegacyWallGraph, Section())).LegacyWallGraph!;
        Assert.AreEqual(2, section.Products.Entries.Count);
        Assert.AreEqual(new ResourceKey((ResourceType)0x3F0C529A, 0, 0x11), section.Products.Entries[1].Key);
        Assert.AreEqual(2U, section.Products.Entries[1].Id);
        Assert.AreEqual(2U, section.WallPatterns.Entries.Single().ReferenceCount);
        var edge = section.Edges.Single();
        Assert.AreEqual(0x00204081U, edge.Code);
        var part = edge.Parts.Single();
        Assert.AreEqual((ushort)1, part.Unknown1);
        Assert.AreEqual((ushort)2, part.Unknown2);
        Assert.AreEqual((ushort)512, part.Unknown3);
        Assert.AreEqual(1U, part.WallPattern1Id);
        Assert.AreEqual(1U, part.ProductId);
        var productEdge = section.ProductEdges1.Single();
        Assert.AreEqual(1U, productEdge.Unknown1);
        Assert.AreEqual(0xCCCCCCCCU, productEdge.Unknown5);
        Assert.AreEqual(2U, productEdge.ProductId);
        Assert.AreEqual(0, section.ProductEdges2.Count);
    }

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.LegacyWallGraph, [.. Section(), 0])));
}
