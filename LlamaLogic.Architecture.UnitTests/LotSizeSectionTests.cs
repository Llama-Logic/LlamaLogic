namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class LotSizeSectionTests
{
    [TestMethod]
    public void DecodeAndEncodeRoundTrip()
    {
        var data = TestBundles.LotSize(30, 20, 3);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotSize, data));
        Assert.AreEqual((byte)30, architecture.LotSize!.SizeX);
        Assert.AreEqual((byte)20, architecture.LotSize.SizeZ);
        Assert.AreEqual((byte)3, architecture.LotSize.LevelCount);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.LotSize));
    }

    [TestMethod]
    [DataRow(1U)]
    [DataRow(14U)]
    public void DecodeKeepsUnsupportedVersionsUndecoded(uint version) =>
        TestBundles.AssertUndecoded(LotArchitectureSectionType.LotSize, TestBundles.LotSize(30, 20, 3, version), architecture => architecture.LotSize);

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.LotSize, [.. TestBundles.LotSize(30, 20, 3), 0])));

    [TestMethod]
    public void EncodeRejectsDimensionsInVersion2()
    {
        var architecture = new LotArchitecture { LotSize = new LotSizeSection { Version = 2, SizeX = 20 } };
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void Version2HasNoDimensions()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotSize, BitConverter.GetBytes(2U)));
        Assert.IsFalse(architecture.LotSize!.HasDimensions);
        CollectionAssert.AreEqual(BitConverter.GetBytes(2U), TestBundles.Encode(architecture, LotArchitectureSectionType.LotSize));
    }
}
