namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class VertexGraphsSectionTests
{
    public static byte[] Section(uint version, int? graphCount = null, uint edgeTag = 0x53454354)
    {
        var bytes = new TestBytes().U32(version);
        for (var i = 0; i < (graphCount ?? (version < 8 ? 3 : 4)); ++i)
            bytes.U32(5)
                .Add("TREV"u8.ToArray()).I32(2).I32(0x030201).U8(3).U8(2).U8(1).I32(0x0402FF).U8(4).U8(2).S8(-1)
                .U32(edgeTag).I32(1).I32(0x03020110).I32(0x030201).I32(-1);
        return bytes.ToArray();
    }

    [TestMethod]
    [DataRow(5U)]
    [DataRow(6U)]
    [DataRow(7U)]
    [DataRow(8U)]
    [DataRow(9U)]
    public void DecodeAndEncodeRoundTrip(uint version)
    {
        var data = Section(version);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.VertexGraphs, data));
        Assert.AreEqual(version, architecture.VertexGraphs!.Version);
        Assert.AreEqual(version < 8 ? 3 : 4, architecture.VertexGraphs.Graphs.Count);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.VertexGraphs));
    }

    [TestMethod]
    [DataRow(4U)]
    [DataRow(10U)]
    public void DecodeKeepsUnsupportedVersionsUndecoded(uint version) =>
        TestBundles.AssertUndecoded(LotArchitectureSectionType.VertexGraphs, Section(version, 4), architecture => architecture.VertexGraphs);

    [TestMethod]
    public void DecodeReadsFields()
    {
        var graph = TestBundles.Decode((LotArchitectureSectionType.VertexGraphs, Section(9))).VertexGraphs!.Graphs[3];
        Assert.AreEqual(5U, graph.Unknown1);
        Assert.AreEqual(2, graph.Vertices.Count);
        Assert.AreEqual(0x0402FF, graph.Vertices[1].Id);
        Assert.AreEqual((byte)4, graph.Vertices[1].Unknown1);
        Assert.AreEqual((byte)2, graph.Vertices[1].Unknown2);
        Assert.AreEqual((sbyte)-1, graph.Vertices[1].Unknown3);
        var edge = graph.Edges.Single();
        Assert.AreEqual(0x03020110, edge.Id);
        Assert.AreEqual(0x030201, edge.Vertex1Id);
        Assert.AreEqual(-1, edge.Vertex2Id);
    }

    [TestMethod]
    public void DecodeRejectsMissingTag() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.VertexGraphs, Section(9, edgeTag: 0))));

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.VertexGraphs, Section(7, 4))));

    [TestMethod]
    public void EncodeRejectsWrongGraphCount()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.VertexGraphs, Section(9)));
        architecture.VertexGraphs!.Version = 7;
        Assert.ThrowsExactly<InvalidOperationException>(() => TestBundles.Encode(architecture, LotArchitectureSectionType.VertexGraphs));
    }

    [TestMethod]
    [DataRow(4U)]
    [DataRow(10U)]
    public void SetVersionRejectsUnsupportedVersions(uint version) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new VertexGraphsSection { Version = version });
}
