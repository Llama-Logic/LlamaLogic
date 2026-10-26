namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class LegacyFencePointsSectionTests
{
    static byte[] Section(uint version = 2) =>
        new TestBytes().U32(version)
            .I32(2).U32(1).F32(12).F32(30).KeyTypeGroupInstance(0x0418FE2A, 0, 0x10).U32(0).F32(13).F32(30).KeyTypeGroupInstance(0x0418FE2A, 0, 0x10)
            .I32(1).U32(2).F32(4).F32(5).KeyTypeGroupInstance(0x3F0C529A, 0, 0x20)
            .ToArray();

    [TestMethod]
    public void DecodeAndEncodeRoundTrip()
    {
        var data = Section();
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LegacyFencePoints, data));
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.LegacyFencePoints));
    }

    [TestMethod]
    [DataRow(1U)]
    [DataRow(3U)]
    public void DecodeKeepsUnsupportedVersionsUndecoded(uint version) =>
        TestBundles.AssertUndecoded(LotArchitectureSectionType.LegacyFencePoints, Section(version), architecture => architecture.LegacyFencePoints);

    [TestMethod]
    public void DecodeReadsFields()
    {
        var section = TestBundles.Decode((LotArchitectureSectionType.LegacyFencePoints, Section())).LegacyFencePoints!;
        Assert.AreEqual(2, section.Points1.Count);
        Assert.AreEqual(1U, section.Points1[0].Unknown1);
        Assert.AreEqual(12f, section.Points1[0].Unknown2);
        Assert.AreEqual(30f, section.Points1[0].Unknown3);
        Assert.AreEqual(new ResourceKey((ResourceType)0x0418FE2A, 0, 0x10), section.Points1[0].Key);
        Assert.AreEqual(new ResourceKey((ResourceType)0x3F0C529A, 0, 0x20), section.Points2.Single().Key);
    }

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.LegacyFencePoints, [.. Section(), 0])));

    [TestMethod]
    public void EncodeWritesNewSection()
    {
        var section = new LegacyFencePointsSection();
        section.Points2.Add(new LegacyFencePoint { Unknown1 = 4, Unknown2 = 1, Unknown3 = 2, Key = new ResourceKey((ResourceType)0x3F0C529A, 0, 7) });
        var expected = new TestBytes().U32(2).I32(0).I32(1).U32(4).F32(1).F32(2).KeyTypeGroupInstance(0x3F0C529A, 0, 7).ToArray();
        CollectionAssert.AreEqual(expected, TestBundles.Encode(new LotArchitecture { LegacyFencePoints = section }, LotArchitectureSectionType.LegacyFencePoints));
    }
}
